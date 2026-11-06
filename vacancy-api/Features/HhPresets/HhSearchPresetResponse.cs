using Vacancy.Api.Features.HhJobs;

namespace Vacancy.Api.Features.HhPresets;

/// <summary>Saved HH search preset returned by the API.</summary>
public sealed record HhSearchPresetResponse(
    Guid Id,
    string Key,
    string Label,
    string Query,
    HhJobExclusionsRequest? DefaultExclusions,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
