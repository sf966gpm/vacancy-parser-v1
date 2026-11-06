using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Vacancy.Api.Features.Presets;

/// <summary>Request to update an existing search preset.</summary>
public sealed record UpdateSearchPresetRequest
{
    /// <summary>Platform this preset searches, such as hh.</summary>
    [Required, StringLength(32)]
    public required string Platform { get; init; }

    /// <summary>Human-readable preset name shown in the dashboard.</summary>
    [Required, StringLength(200)]
    public required string Name { get; init; }

    /// <summary>Search text interpreted by the selected platform.</summary>
    [Required, StringLength(500)]
    public required string Query { get; init; }

    /// <summary>Platform-specific search settings.</summary>
    public JsonElement? Settings { get; init; }
}
