using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Vacancy.Api.Features.Vacancies;

namespace Vacancy.Api.Messaging.RabbitMq;

public sealed class HhVacancyBatchConsumer(
    IConfiguration configuration,
    IServiceScopeFactory scopeFactory,
    ILogger<HhVacancyBatchConsumer> logger) : BackgroundService
{
    private const string DeadLetterExchange = "collector.dead-letter";
    private const string QueueNameFallback = "collector.hh.vacancies";
    private const string DeadLetterRoutingKey = "collector.hh.vacancies.failed";
    private const string DeadLetterQueue = "collector.hh.vacancies.failed";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = configuration["RABBITMQ_HOST"] ?? "localhost",
            Port = int.TryParse(configuration["RABBITMQ_BROKER_PORT"], out var port) ? port : 5672,
            UserName = configuration["RABBITMQ_USER"] ?? "guest",
            Password = configuration["RABBITMQ_PASSWORD"] ?? "guest",
            AutomaticRecoveryEnabled = true,
            ClientProvidedName = "vacancy-parser:hh-vacancy-consumer"
        };

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        var queueName = configuration["RABBITMQ_VACANCY_QUEUE"] ?? QueueNameFallback;

        await channel.ExchangeDeclareAsync(
            DeadLetterExchange,
            ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(
            DeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);
        await channel.QueueBindAsync(
            DeadLetterQueue,
            DeadLetterExchange,
            DeadLetterRoutingKey,
            cancellationToken: stoppingToken);

        var queueArguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = DeadLetterExchange,
            ["x-dead-letter-routing-key"] = DeadLetterRoutingKey
        };
        await channel.QueueDeclareAsync(
            queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArguments,
            cancellationToken: stoppingToken);
        await channel.BasicQosAsync(0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
            await HandleDeliveryAsync(channel, eventArgs, stoppingToken);
        await channel.BasicConsumeAsync(
            queueName,
            autoAck: false,
            consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation("Consuming HH vacancy batches from {QueueName}.", queueName);

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The host is shutting down.
        }
    }

    private async Task HandleDeliveryAsync(
        IChannel channel,
        BasicDeliverEventArgs eventArgs,
        CancellationToken cancellationToken)
    {
        HhVacancyBatchMessage? batch;
        try
        {
            var body = eventArgs.Body.ToArray();
            batch = JsonSerializer.Deserialize<HhVacancyBatchMessage>(body, JsonOptions);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Received invalid HH vacancy batch JSON; moving it to the dead-letter queue.");
            await channel.BasicNackAsync(
                eventArgs.DeliveryTag,
                multiple: false,
                requeue: false,
                cancellationToken: cancellationToken);
            return;
        }

        if (batch is null)
        {
            logger.LogWarning("Received an empty HH vacancy batch; moving it to the dead-letter queue.");
            await channel.BasicNackAsync(
                eventArgs.DeliveryTag,
                multiple: false,
                requeue: false,
                cancellationToken: cancellationToken);
            return;
        }

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var ingestionService = scope.ServiceProvider.GetRequiredService<IHhVacancyIngestionService>();
            await ingestionService.ImportBatchAsync(batch, cancellationToken);
            await channel.BasicAckAsync(
                eventArgs.DeliveryTag,
                multiple: false,
                cancellationToken: cancellationToken);
            logger.LogInformation(
                "Stored {VacancyCount} HH vacancies from parse job {JobId}.",
                batch.Vacancies.Count,
                batch.JobId);
        }
        catch (InvalidHhVacancyBatchException exception)
        {
            logger.LogWarning(exception, "Received an invalid HH vacancy batch; moving it to the dead-letter queue.");
            await channel.BasicNackAsync(
                eventArgs.DeliveryTag,
                multiple: false,
                requeue: false,
                cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not store an HH vacancy batch; requeuing it.");
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            await channel.BasicNackAsync(
                eventArgs.DeliveryTag,
                multiple: false,
                requeue: true,
                cancellationToken: cancellationToken);
        }
    }
}
