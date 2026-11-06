using Microsoft.EntityFrameworkCore;
using Vacancy.Api.Persistence;
using Vacancy.Api.Persistence.Entities;

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
                if (!await DispatchNextAsync(stoppingToken))
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(_waitingForSchema ? 10 : 1),
                        stoppingToken);
                    continue;
                }

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

    private async Task<bool> DispatchNextAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!await HasMigrationHistoryTableAsync(dbContext, cancellationToken))
        {
            LogWaitingForSchema();
            return false;
        }

        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
        if (pendingMigrations.Any())
        {
            LogWaitingForSchema();
            return false;
        }

        _waitingForSchema = false;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var messages = await dbContext.OutboxMessages
            .FromSqlInterpolated($"""
                SELECT * FROM "outbox_messages"
                WHERE "ProcessedAt" IS NULL AND "AvailableAt" <= {DateTimeOffset.UtcNow}
                ORDER BY "CreatedAt"
                LIMIT 1
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);
        var message = messages.SingleOrDefault();
        if (message is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return false;
        }

        try
        {
            await publisher.PublishAsync(message.JobId, message.PayloadJson, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            await RecordPublishFailureAsync(dbContext, message, exception, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }

        var now = DateTimeOffset.UtcNow;
        message.ProcessedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.ParseJobs
            .Where(job => job.Id == message.JobId && job.Status == "pending")
            .ExecuteUpdateAsync(updates => updates
                .SetProperty(job => job.Status, "queued")
                .SetProperty(job => job.QueuedAt, now), cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Published HH parse job {JobId} to RabbitMQ.", message.JobId);
        return true;
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

    private async Task RecordPublishFailureAsync(
        AppDbContext dbContext,
        OutboxMessageEntity outboxMessage,
        Exception exception,
        CancellationToken cancellationToken)
    {
        outboxMessage.Attempts++;
        outboxMessage.LastError = exception.GetType().Name;

        if (outboxMessage.Attempts >= MaximumAttempts)
        {
            var now = DateTimeOffset.UtcNow;
            outboxMessage.ProcessedAt = now;
            await dbContext.ParseJobs
                .Where(job => job.Id == outboxMessage.JobId && job.Status == "pending")
                .ExecuteUpdateAsync(updates => updates
                    .SetProperty(job => job.Status, "failed")
                    .SetProperty(job => job.CompletedAt, now)
                    .SetProperty(job => job.FailureReason, "Could not publish the job to RabbitMQ after several attempts."),
                    cancellationToken);
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
}
