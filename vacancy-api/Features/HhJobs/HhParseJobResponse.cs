namespace Vacancy.Api.Features.HhJobs;

/// <summary>Saved HH parse job and its latest known processing status.</summary>
public sealed record HhParseJobResponse(
    Guid Id,
    string PresetKey,
    string PresetLabel,
    string Query,
    HhJobExclusionsRequest? Exclusions,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? QueuedAt,
    DateTimeOffset? CompletedAt,
    string? FailureReason);
