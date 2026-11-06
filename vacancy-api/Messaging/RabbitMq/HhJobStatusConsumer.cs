using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Vacancy.Api.Features.HhJobs;

namespace Vacancy.Api.Messaging.RabbitMq;

public sealed class HhJobStatusConsumer(
    IConfiguration configuration,
    IServiceScopeFactory scopeFactory,
    ILogger<HhJobStatusConsumer> logger) : BackgroundService
{
    private const string DeadLetterExchange = "collector.dead-letter";
    private const string QueueNameFallback = "collector.hh.job-status";
    private const string DeadLetterRoutingKey = "collector.hh.job-status.failed";
    private const string DeadLetterQueue = "collector.hh.job-status.failed";
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
            ClientProvidedName = "vacancy-parser:hh-job-status-consumer"
        };

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        var queueName = configuration["RABBITMQ_JOB_STATUS_QUEUE"] ?? QueueNameFallback;

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

        logger.LogInformation("Consuming HH job status events from {QueueName}.", queueName);

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
        HhParseJobStatusMessage? message;
        try
        {
            message = JsonSerializer.Deserialize<HhParseJobStatusMessage>(eventArgs.Body.ToArray(), JsonOptions);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Received invalid HH job status JSON; moving it to the dead-letter queue.");
            await RejectToDeadLetterQueueAsync(channel, eventArgs.DeliveryTag, cancellationToken);
            return;
        }

        if (message is null)
        {
            logger.LogWarning("Received an empty HH job status event; moving it to the dead-letter queue.");
            await RejectToDeadLetterQueueAsync(channel, eventArgs.DeliveryTag, cancellationToken);
            return;
        }

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var statusService = scope.ServiceProvider.GetRequiredService<IHhJobStatusService>();
            var applied = await statusService.ApplyTerminalStatusAsync(message, cancellationToken);
            if (!applied)
            {
                logger.LogWarning(
                    "Received invalid or unknown HH job status event for {JobId}; moving it to the dead-letter queue.",
                    message.JobId);
                await RejectToDeadLetterQueueAsync(channel, eventArgs.DeliveryTag, cancellationToken);
                return;
            }

            await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken);
            logger.LogInformation(
                "Recorded HH parse job {JobId} as {Status}.",
                message.JobId,
                message.Status);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not store HH job status for {JobId}; requeuing it.", message.JobId);
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            await channel.BasicNackAsync(
                eventArgs.DeliveryTag,
                multiple: false,
                requeue: true,
                cancellationToken: cancellationToken);
        }
    }

    private async Task RejectToDeadLetterQueueAsync(
        IChannel channel,
        ulong deliveryTag,
        CancellationToken cancellationToken)
    {
        await channel.BasicNackAsync(
            deliveryTag,
            multiple: false,
            requeue: false,
            cancellationToken: cancellationToken);
    }
}
