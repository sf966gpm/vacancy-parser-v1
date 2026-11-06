using System.Text.Json;

namespace Vacancy.Api.Features.Presets;

/// <summary>Saved search preset for a vacancy platform.</summary>
public sealed record SearchPresetResponse(
    Guid Id,
    string Platform,
    string Name,
    string Query,
    JsonElement? Settings,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
