using System.ComponentModel.DataAnnotations;

namespace Vacancy.Api.Features.HhJobs;

/// <summary>Request to enqueue an HH vacancy parsing job.</summary>
public sealed record CreateHhParseJobRequest
{
    /// <summary>Key of the saved HH search preset to use.</summary>
    [Required, StringLength(100)]
    public required string PresetKey { get; init; }

    /// <summary>Optional exclusions that override the preset defaults for this job.</summary>
    public HhJobExclusionsRequest? Exclusions { get; init; }
}
