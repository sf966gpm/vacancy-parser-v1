using Microsoft.EntityFrameworkCore;
using Vacancy.Api.Persistence;

namespace Vacancy.Api.Features.HhJobs;

public sealed class HhJobStatusService(AppDbContext dbContext) : IHhJobStatusService
{
    private const string Platform = "hh";

    public async Task<bool> ApplyTerminalStatusAsync(
        HhParseJobStatusMessage message,
        CancellationToken cancellationToken)
    {
        if (message.JobId == Guid.Empty
            || (message.Status != "completed" && message.Status != "failed")
            || message.CompletedAt == default
            || message.FailureReason?.Length > 2000)
        {
            return false;
        }

        var status = message.Status;
        var completedAt = message.CompletedAt.ToUniversalTime();
        var failureReason = status == "failed" ? message.FailureReason : null;

        var updatedRows = await dbContext.ParseJobs
            .Where(job => job.Id == message.JobId
                && job.Platform == Platform
                && job.Status != "completed"
                && job.Status != "failed")
            .ExecuteUpdateAsync(updates => updates
                .SetProperty(job => job.Status, status)
                .SetProperty(job => job.QueuedAt, job => job.QueuedAt ?? completedAt)
                .SetProperty(job => job.CompletedAt, completedAt)
                .SetProperty(job => job.FailureReason, failureReason), cancellationToken);

        if (updatedRows > 0)
        {
            return true;
        }

        // A terminal status may already have been recorded when RabbitMQ redelivers an event.
        return await dbContext.ParseJobs
            .AnyAsync(job => job.Id == message.JobId && job.Platform == Platform, cancellationToken);
    }
}
