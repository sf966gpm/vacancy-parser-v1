using System.ComponentModel.DataAnnotations;

namespace Vacancy.Api.Features.HhJobs;

/// <summary>HH vacancy search terms selected for a parsing job.</summary>
public sealed record HhSearchPresetRequest
{
    /// <summary>Stable key identifying the preset.</summary>
    [Required, StringLength(100)]
    public required string Key { get; init; }

    /// <summary>Human-readable preset name.</summary>
    [Required, StringLength(200)]
    public required string Label { get; init; }

    /// <summary>Text entered into the HH vacancy search.</summary>
    [Required, StringLength(500)]
    public required string Query { get; init; }
}
