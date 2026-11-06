using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vacancy.Api.Persistence;
using Vacancy.Api.Persistence.Entities;

namespace Vacancy.Api.Features.Presets;

public sealed class SearchPresetService(AppDbContext dbContext) : ISearchPresetService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<SearchPresetResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var presets = await dbContext.SearchPresets
            .AsNoTracking()
            .OrderBy(preset => preset.Platform)
            .ThenBy(preset => preset.Name)
            .ToListAsync(cancellationToken);

        return presets.Select(ToResponse).ToArray();
    }

    public async Task<SearchPresetResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var preset = await dbContext.SearchPresets
            .AsNoTracking()
            .SingleOrDefaultAsync(preset => preset.Id == id, cancellationToken);

        return preset is null ? null : ToResponse(preset);
    }

    public async Task<SearchPresetResponse> CreateAsync(
        CreateSearchPresetRequest request,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var preset = new SearchPresetEntity
        {
            Id = Guid.NewGuid(),
            Platform = request.Platform.Trim().ToLowerInvariant(),
            Name = request.Name.Trim(),
            Query = request.Query.Trim(),
            SettingsJson = SerializeSettings(request.Settings),
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.SearchPresets.Add(preset);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(preset);
    }

    public async Task<SearchPresetResponse?> UpdateAsync(
        Guid id,
        UpdateSearchPresetRequest request,
        CancellationToken cancellationToken)
    {
        var preset = await dbContext.SearchPresets
            .SingleOrDefaultAsync(preset => preset.Id == id, cancellationToken);
        if (preset is null)
        {
            return null;
        }

        preset.Platform = request.Platform.Trim().ToLowerInvariant();
        preset.Name = request.Name.Trim();
        preset.Query = request.Query.Trim();
        preset.SettingsJson = SerializeSettings(request.Settings);
        preset.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(preset);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var preset = await dbContext.SearchPresets
            .SingleOrDefaultAsync(preset => preset.Id == id, cancellationToken);
        if (preset is null)
        {
            return false;
        }

        dbContext.SearchPresets.Remove(preset);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static SearchPresetResponse ToResponse(SearchPresetEntity preset) => new(
        preset.Id,
        preset.Platform,
        preset.Name,
        preset.Query,
        DeserializeSettings(preset.SettingsJson),
        preset.CreatedAt,
        preset.UpdatedAt);

    private static string? SerializeSettings(JsonElement? settings) =>
        settings is null || settings.Value.ValueKind == JsonValueKind.Null
            ? null
            : JsonSerializer.Serialize(settings.Value, JsonOptions);

    private static JsonElement? DeserializeSettings(string? json)
    {
        if (json is null)
        {
            return null;
        }

        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
