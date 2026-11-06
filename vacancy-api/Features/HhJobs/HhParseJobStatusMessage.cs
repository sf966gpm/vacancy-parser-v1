namespace Vacancy.Api.Features.HhJobs;

/// <summary>Terminal result reported by the HH collector.</summary>
public sealed record HhParseJobStatusMessage(
    Guid JobId,
    string Status,
    DateTimeOffset CompletedAt,
    string? FailureReason);
