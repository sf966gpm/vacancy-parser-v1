namespace Collector.Playwright.Hh.Jobs;

public sealed record HhJobStatusMessage(
    Guid JobId,
    string Status,
    DateTimeOffset CompletedAt,
    string? FailureReason);
