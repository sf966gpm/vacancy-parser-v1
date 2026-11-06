using System.ComponentModel.DataAnnotations;
using Vacancy.Api.Features.HhJobs;

namespace Vacancy.Api.Features.HhPresets;

/// <summary>Request to update an existing HH vacancy search preset.</summary>
public sealed record UpdateHhSearchPresetRequest
{
    /// <summary>Human-readable name shown in the dashboard.</summary>
    [Required, StringLength(200)]
    public required string Label { get; init; }

    /// <summary>Search text sent to HH.</summary>
    [Required, StringLength(500)]
    public required string Query { get; init; }

    /// <summary>Default exclusions applied to jobs using this preset.</summary>
    public HhJobExclusionsRequest? DefaultExclusions { get; init; }
}
