using System.ComponentModel.DataAnnotations;
using Vacancy.Api.Features.HhJobs;

namespace Vacancy.Api.Features.HhPresets;

/// <summary>Request to save an HH vacancy search preset.</summary>
public sealed record CreateHhSearchPresetRequest
{
    /// <summary>Stable key used when creating a parse job.</summary>
    [Required, StringLength(100)]
    public required string Key { get; init; }

    /// <summary>Human-readable name shown in the dashboard.</summary>
    [Required, StringLength(200)]
    public required string Label { get; init; }

    /// <summary>Search text sent to HH.</summary>
    [Required, StringLength(500)]
    public required string Query { get; init; }

    /// <summary>Default exclusions applied to jobs using this preset.</summary>
    public HhJobExclusionsRequest? DefaultExclusions { get; init; }
}
