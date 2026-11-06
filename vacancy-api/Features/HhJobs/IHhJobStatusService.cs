namespace Vacancy.Api.Features.HhJobs;

public interface IHhJobStatusService
{
    Task<bool> ApplyTerminalStatusAsync(
        HhParseJobStatusMessage message,
        CancellationToken cancellationToken);
}
