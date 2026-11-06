using System.Text;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;

namespace Vacancy.Api.Messaging.RabbitMq;

public sealed class HhJobPublisher : IHhJobPublisher, IHostedService, IAsyncDisposable
{
    private const string DeadLetterExchange = "collector.dead-letter";
    private const string DeadLetterQueue = "collector.parse.failed";
    private const string DeadLetterRoutingKey = "collector.parse.failed";

    private readonly ConnectionFactory _connectionFactory;
    private readonly string _queueName;
    private IConnection? _connection;

    public HhJobPublisher(IConfiguration configuration)
    {
        _queueName = configuration["RABBITMQ_QUEUE"] ?? "collector.parse";
        _connectionFactory = new ConnectionFactory
        {
            HostName = configuration["RABBITMQ_HOST"] ?? "localhost",
            Port = int.TryParse(configuration["RABBITMQ_BROKER_PORT"], out var port) ? port : 5672,
            UserName = configuration["RABBITMQ_USER"] ?? "guest",
            Password = configuration["RABBITMQ_PASSWORD"] ?? "guest",
            AutomaticRecoveryEnabled = true,
            ClientProvidedName = "vacancy-parser:api-publisher"
        };
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_connection is not null)
        {
            await _connection.CloseAsync(cancellationToken);
        }
    }

    public async Task PublishAsync(Guid messageId, string payloadJson, CancellationToken cancellationToken)
    {
        if (_connection is null)
        {
            throw new InvalidOperationException("The RabbitMQ publisher has not started.");
        }

        try
        {
            var channelOptions = new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true);
            await using var channel = await _connection.CreateChannelAsync(channelOptions, cancellationToken);
            await DeclareTopologyAsync(channel, cancellationToken);

            var body = Encoding.UTF8.GetBytes(payloadJson);
            var properties = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                MessageId = messageId.ToString(),
                Type = "hh.parse"
            };

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: _queueName,
                mandatory: true,
                basicProperties: properties,
                body,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new HhJobQueueUnavailableException(exception);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }

    private async Task DeclareTopologyAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            DeadLetterExchange,
            ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(
            DeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(
            DeadLetterQueue,
            DeadLetterExchange,
            DeadLetterRoutingKey,
            cancellationToken: cancellationToken);

        var queueArguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = DeadLetterExchange,
            ["x-dead-letter-routing-key"] = DeadLetterRoutingKey
        };

        await channel.QueueDeclareAsync(
            _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArguments,
            cancellationToken: cancellationToken);
    }
}
