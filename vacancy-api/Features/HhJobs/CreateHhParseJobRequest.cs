using System.ComponentModel.DataAnnotations;

namespace Vacancy.Api.Features.HhJobs;

/// <summary>Request to enqueue an HH vacancy parsing job.</summary>
public sealed record CreateHhParseJobRequest
{
    /// <summary>Search preset to apply to the HH vacancy search.</summary>
    [Required]
    public required HhSearchPresetRequest Preset { get; init; }

    /// <summary>Vacancies and title categories to exclude from the results.</summary>
    public HhJobExclusionsRequest? Exclusions { get; init; }
}
