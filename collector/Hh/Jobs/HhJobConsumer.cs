using System.Text.Json;
using Collector.Playwright.Browser;
using Microsoft.Playwright;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Collector.Playwright.Hh.Jobs;

public sealed class HhJobConsumer : IAsyncDisposable
{
    private const string DeadLetterExchange = "collector.dead-letter";
    private const string DeadLetterQueue = "collector.parse.failed";
    private const string DeadLetterRoutingKey = "collector.parse.failed";
    private const string StatusDeadLetterQueue = "collector.hh.job-status.failed";
    private const string StatusDeadLetterRoutingKey = "collector.hh.job-status.failed";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly BrowserSession _browserSession;
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly string _queueName;
    private readonly string _statusQueueName;
    private readonly ProcessedHhJobStore _processedJobs;

    private HhJobConsumer(
        BrowserSession browserSession,
        IConnection connection,
        IChannel channel,
        string queueName,
        string statusQueueName,
        ProcessedHhJobStore processedJobs)
    {
        _browserSession = browserSession;
        _connection = connection;
        _channel = channel;
        _queueName = queueName;
        _statusQueueName = statusQueueName;
        _processedJobs = processedJobs;
    }

    public static async Task<HhJobConsumer> ConnectAsync(BrowserSession browserSession)
    {
        var queueName = Environment.GetEnvironmentVariable("RABBITMQ_QUEUE") ?? "collector.parse";
        var statusQueueName = Environment.GetEnvironmentVariable("RABBITMQ_JOB_STATUS_QUEUE")
            ?? "collector.hh.job-status";
        var stateDirectory = Environment.GetEnvironmentVariable("COLLECTOR_STATE_DIRECTORY")
            ?? Path.Combine(AppContext.BaseDirectory, "state");
        var processedJobs = await ProcessedHhJobStore.OpenAsync(stateDirectory);
        var factory = new ConnectionFactory
        {
            HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost",
            Port = int.TryParse(Environment.GetEnvironmentVariable("RABBITMQ_BROKER_PORT"), out var port)
                ? port
                : 5672,
            UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest",
            Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest",
            AutomaticRecoveryEnabled = true,
            ClientProvidedName = "vacancy-parser:playwright-consumer"
        };

        IConnection? connection = null;
        IChannel? channel = null;

        try
        {
            connection = await factory.CreateConnectionAsync();
            var channelOptions = new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true);
            channel = await connection.CreateChannelAsync(channelOptions);
            var jobConsumer = new HhJobConsumer(
                browserSession,
                connection,
                channel,
                queueName,
                statusQueueName,
                processedJobs);
            await jobConsumer.ConfigureAsync();
            return jobConsumer;
        }
        catch
        {
            if (channel is not null)
            {
                await channel.DisposeAsync();
            }

            if (connection is not null)
            {
                await connection.DisposeAsync();
            }

            await processedJobs.DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.DisposeAsync();
        await _connection.DisposeAsync();
        await _processedJobs.DisposeAsync();
    }

    private async Task ConfigureAsync()
    {
        await _channel.ExchangeDeclareAsync(
            DeadLetterExchange,
            ExchangeType.Direct,
            durable: true,
            autoDelete: false);
        await _channel.QueueDeclareAsync(
            DeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false);
        await _channel.QueueBindAsync(DeadLetterQueue, DeadLetterExchange, DeadLetterRoutingKey);
        await _channel.QueueDeclareAsync(
            StatusDeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false);
        await _channel.QueueBindAsync(
            StatusDeadLetterQueue,
            DeadLetterExchange,
            StatusDeadLetterRoutingKey);

        var queueArguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = DeadLetterExchange,
            ["x-dead-letter-routing-key"] = DeadLetterRoutingKey
        };

        await _channel.QueueDeclareAsync(
            _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArguments);
        var statusQueueArguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = DeadLetterExchange,
            ["x-dead-letter-routing-key"] = StatusDeadLetterRoutingKey
        };
        await _channel.QueueDeclareAsync(
            _statusQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: statusQueueArguments);
        await _channel.BasicQosAsync(0, prefetchCount: 1, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += HandleDeliveryAsync;
        await _channel.BasicConsumeAsync(_queueName, autoAck: false, consumer);
    }

    private async Task HandleDeliveryAsync(object sender, BasicDeliverEventArgs eventArgs)
    {
        var messageId = eventArgs.BasicProperties.MessageId;
        if (!Guid.TryParse(messageId, out var jobId) || jobId == Guid.Empty)
        {
            Console.Error.WriteLine("Received a parse job without a valid message ID; moving it to the dead-letter queue.");
            await RejectToDeadLetterQueueAsync(eventArgs.DeliveryTag);
            return;
        }

        var previousResult = await _processedJobs.GetAsync(jobId, CancellationToken.None);
        if (previousResult is not null)
        {
            try
            {
                await PublishStatusMessageAsync(previousResult, CancellationToken.None);
                await _channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
                Console.WriteLine($"[job:{jobId}] Duplicate delivery acknowledged without rerunning the task.");
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"[job:{jobId}] Could not republish stored status; requeuing: {exception.Message}");
                await RequeueAsync(eventArgs.DeliveryTag);
            }

            return;
        }

        // RabbitMQ.Client owns the delivery buffer; copy it before awaiting other work.
        var body = eventArgs.Body.ToArray();
        HhParseJobMessage? job;

        try
        {
            job = JsonSerializer.Deserialize<HhParseJobMessage>(body, JsonOptions);
        }
        catch (JsonException exception)
        {
            Console.Error.WriteLine($"Received invalid JSON; moving message to {DeadLetterQueue}: {exception.Message}");
            if (!await TryReportFailureAsync(jobId, "Collector received invalid job JSON."))
            {
                await RequeueAsync(eventArgs.DeliveryTag);
                return;
            }

            await RejectToDeadLetterQueueAsync(eventArgs.DeliveryTag);
            return;
        }

        if (!TryGetSearchUri(job, out var targetUri, out var validationError))
        {
            Console.Error.WriteLine($"Received invalid parse job; moving message to {DeadLetterQueue}: {validationError}");
            if (!await TryReportFailureAsync(jobId, "Collector rejected the parse job payload."))
            {
                await RequeueAsync(eventArgs.DeliveryTag);
                return;
            }

            await RejectToDeadLetterQueueAsync(eventArgs.DeliveryTag);
            return;
        }

        var validatedJob = job!;
        var preset = validatedJob.Preset!;
        var exclusions = validatedJob.Exclusions;
        Console.WriteLine(
            $"[preset:{preset.Key}] {preset.Label}; " +
            $"excluding {exclusions?.VacancyIds?.Count ?? 0} vacancy IDs and " +
            $"{exclusions?.TitleCategories?.Count ?? 0} title categories.");
        Console.WriteLine($"[preset:{preset.Key}] Opening {targetUri}");

        PageVisitResult result;
        try
        {
            result = await ProcessJobAsync(targetUri, preset.Key!);
            Console.WriteLine(
                $"[preset:{preset.Key}] Finished navigation; HTTP {result.Status?.ToString() ?? "no response"}; title: {result.Title}; URL: {result.Url}");
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"[preset:{preset.Key}] Job failed; moving message to {DeadLetterQueue}: {exception.Message}");
            if (!await TryReportFailureAsync(jobId, "Collector could not process the parse job."))
            {
                await RequeueAsync(eventArgs.DeliveryTag);
                return;
            }

            await RejectToDeadLetterQueueAsync(eventArgs.DeliveryTag);
            return;
        }

        var succeeded = result.Status is >= 200 and < 400;
        var terminalResult = new HhJobStatusMessage(
            jobId,
            succeeded ? "completed" : "failed",
            DateTimeOffset.UtcNow,
            succeeded ? null : $"HH returned HTTP {result.Status?.ToString() ?? "no response"}.");
        try
        {
            await RecordAndPublishStatusAsync(terminalResult);
            await _channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"[job:{jobId}] Could not publish terminal status; requeuing: {exception.Message}");
            await RequeueAsync(eventArgs.DeliveryTag);
        }
    }

    private async Task<bool> TryReportFailureAsync(Guid jobId, string failureReason)
    {
        try
        {
            await RecordAndPublishStatusAsync(new HhJobStatusMessage(
                jobId,
                "failed",
                DateTimeOffset.UtcNow,
                failureReason));
            return true;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"[job:{jobId}] Could not publish failure status: {exception.Message}");
            return false;
        }
    }

    private async Task RecordAndPublishStatusAsync(HhJobStatusMessage result)
    {
        var storedResult = await _processedJobs.RecordAsync(result, CancellationToken.None);
        await PublishStatusMessageAsync(storedResult, CancellationToken.None);
    }

    private async Task PublishStatusMessageAsync(HhJobStatusMessage message, CancellationToken cancellationToken)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(message, JsonOptions);
        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = message.JobId.ToString(),
            Type = "hh.job.status"
        };

        await _channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: _statusQueueName,
            mandatory: true,
            basicProperties: properties,
            body,
            cancellationToken);
    }

    private async Task RequeueAsync(ulong deliveryTag)
    {
        await Task.Delay(TimeSpan.FromSeconds(5));
        await _channel.BasicNackAsync(deliveryTag, multiple: false, requeue: true);
    }

    private async Task RejectToDeadLetterQueueAsync(ulong deliveryTag)
    {
        await _channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false);
    }

    private async Task<PageVisitResult> ProcessJobAsync(Uri targetUri, string presetKey)
    {
        IPage? page = null;

        try
        {
            page = await _browserSession.OpenTaskPageAsync();
            return await NavigateAndInspectAsync(page, targetUri);
        }
        catch (PlaywrightException exception) when (page?.IsClosed == true)
        {
            Console.Error.WriteLine(
                $"[preset:{presetKey}] Browser closed during the task; reopening the browser and retrying once: {exception.Message}");
            page = await _browserSession.ReopenBrowserAndOpenTaskPageAsync();
            return await NavigateAndInspectAsync(page, targetUri);
        }
        finally
        {
            if (page is not null)
            {
                await _browserSession.CloseTaskPageAsync(page);
            }
        }
    }

    private static async Task<PageVisitResult> NavigateAndInspectAsync(IPage page, Uri targetUri)
    {
        var response = await page.GotoAsync(targetUri.ToString(), new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 60_000
        });
        var title = await page.TitleAsync();

        return new PageVisitResult(response?.Status, title, page.Url);
    }

    private sealed record PageVisitResult(int? Status, string Title, string Url);

    private static bool TryGetSearchUri(
        HhParseJobMessage? job,
        out Uri targetUri,
        out string validationError)
    {
        targetUri = null!;

        if (job is null)
        {
            validationError = "Message body must contain a JSON object.";
            return false;
        }

        if (job.Preset is null
            || string.IsNullOrWhiteSpace(job.Preset.Key)
            || string.IsNullOrWhiteSpace(job.Preset.Label)
            || string.IsNullOrWhiteSpace(job.Preset.Query))
        {
            validationError = "Preset must contain non-empty key, label, and query.";
            return false;
        }

        if (job.Exclusions?.VacancyIds?.Any(id => id <= 0) == true
            || job.Exclusions?.TitleCategories?.Any(string.IsNullOrWhiteSpace) == true)
        {
            validationError = "Exclusions must contain positive vacancy IDs and non-empty title category names.";
            return false;
        }

        targetUri = new UriBuilder(Uri.UriSchemeHttps, "hh.ru")
        {
            Path = "/search/vacancy",
            Query = BuildSearchQuery(job.Preset.Query)
        }.Uri;
        validationError = string.Empty;
        return true;
    }

    private static string BuildSearchQuery(string query)
    {
        KeyValuePair<string, string>[] searchParameters =
        [
            new("text", query),
            new("area", "113"),
            new("search_field", "name"),
            new("search_field", "description"),
            new("schedule", "remote"),
            new("search_period", "3"),
            new("order_by", "publication_time")
        ];

        return string.Join(
            "&",
            searchParameters.Select(parameter =>
                $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}"));
    }

}
