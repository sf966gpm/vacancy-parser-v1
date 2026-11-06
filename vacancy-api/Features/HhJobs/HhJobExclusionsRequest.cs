using System.ComponentModel.DataAnnotations;

namespace Vacancy.Api.Features.HhJobs;

/// <summary>Optional vacancy IDs and title categories to exclude.</summary>
public sealed record HhJobExclusionsRequest : IValidatableObject
{
    /// <summary>HH vacancy IDs that should be excluded.</summary>
    public IReadOnlyList<long>? VacancyIds { get; init; }

    /// <summary>Title categories that should be excluded.</summary>
    public IReadOnlyList<string>? TitleCategories { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (VacancyIds?.Any(id => id <= 0) == true)
        {
            yield return new ValidationResult(
                "Vacancy IDs must be positive numbers.",
                [nameof(VacancyIds)]);
        }

        if (TitleCategories?.Any(string.IsNullOrWhiteSpace) == true)
        {
            yield return new ValidationResult(
                "Title categories must not be empty.",
                [nameof(TitleCategories)]);
        }
    }
}
