using Microsoft.EntityFrameworkCore;
using Vacancy.Api.Persistence;

namespace Vacancy.Api.Messaging.RabbitMq;

public sealed class HhJobOutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    IHhJobPublisher publisher,
    ILogger<HhJobOutboxDispatcher> logger) : BackgroundService
{
    private const int MaximumAttempts = 10;
    private bool _waitingForSchema;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = await GetNextMessageAsync(stoppingToken);
                if (message is null)
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(_waitingForSchema ? 10 : 1),
                        stoppingToken);
                    continue;
                }

                await DispatchAsync(message, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Could not read an HH job from the outbox.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task<PendingOutboxMessage?> GetNextMessageAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!await HasMigrationHistoryTableAsync(dbContext, cancellationToken))
        {
            LogWaitingForSchema();
            return null;
        }

        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
        if (pendingMigrations.Any())
        {
            LogWaitingForSchema();
            return null;
        }

        _waitingForSchema = false;
        return await dbContext.OutboxMessages
            .AsNoTracking()
            .Where(message => message.ProcessedAt == null && message.AvailableAt <= DateTimeOffset.UtcNow)
            .OrderBy(message => message.CreatedAt)
            .Select(message => new PendingOutboxMessage(message.Id, message.JobId, message.PayloadJson))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<bool> HasMigrationHistoryTableAsync(
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = dbContext.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT to_regclass('public.\"__EFMigrationsHistory\"') IS NOT NULL;";
            return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }

    private void LogWaitingForSchema()
    {
        if (_waitingForSchema)
        {
            return;
        }

        logger.LogWarning(
            "PostgreSQL migrations are pending. Run `docker compose exec vacancy-api dotnet Vacancy.Api.dll --migrate` before dispatching jobs.");
        _waitingForSchema = true;
    }

    private async Task DispatchAsync(PendingOutboxMessage message, CancellationToken cancellationToken)
    {
        try
        {
            await publisher.PublishAsync(message.JobId, message.PayloadJson, cancellationToken);

            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var outboxMessage = await dbContext.OutboxMessages
                .Include(outbox => outbox.Job)
                .SingleAsync(outbox => outbox.Id == message.Id, cancellationToken);
            var now = DateTimeOffset.UtcNow;
            outboxMessage.ProcessedAt = now;
            outboxMessage.Job.Status = "queued";
            outboxMessage.Job.QueuedAt = now;
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Published HH parse job {JobId} to RabbitMQ.", message.JobId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            await RecordPublishFailureAsync(message.Id, exception, cancellationToken);
        }
    }

    private async Task RecordPublishFailureAsync(
        Guid outboxMessageId,
        Exception exception,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var outboxMessage = await dbContext.OutboxMessages
            .Include(outbox => outbox.Job)
            .SingleOrDefaultAsync(outbox => outbox.Id == outboxMessageId, cancellationToken);
        if (outboxMessage is null)
        {
            return;
        }

        outboxMessage.Attempts++;
        outboxMessage.LastError = exception.GetType().Name;

        if (outboxMessage.Attempts >= MaximumAttempts)
        {
            var now = DateTimeOffset.UtcNow;
            outboxMessage.ProcessedAt = now;
            outboxMessage.Job.Status = "failed";
            outboxMessage.Job.CompletedAt = now;
            outboxMessage.Job.FailureReason = "Could not publish the job to RabbitMQ after several attempts.";
            logger.LogError(
                exception,
                "HH parse job {JobId} exceeded the outbox retry limit.",
                outboxMessage.JobId);
        }
        else
        {
            var retryDelaySeconds = Math.Min(60, 1 << Math.Min(outboxMessage.Attempts, 6));
            outboxMessage.AvailableAt = DateTimeOffset.UtcNow.AddSeconds(retryDelaySeconds);
            logger.LogWarning(
                exception,
                "Could not publish HH parse job {JobId}; retrying in {DelaySeconds} seconds.",
                outboxMessage.JobId,
                retryDelaySeconds);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private sealed record PendingOutboxMessage(Guid Id, Guid JobId, string PayloadJson);
}
