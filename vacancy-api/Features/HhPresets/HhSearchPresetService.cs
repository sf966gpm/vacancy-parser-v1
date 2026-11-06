using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vacancy.Api.Features.HhJobs;
using Vacancy.Api.Persistence;
using Vacancy.Api.Persistence.Entities;

namespace Vacancy.Api.Features.HhPresets;

public sealed class HhSearchPresetService(AppDbContext dbContext) : IHhSearchPresetService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<HhSearchPresetResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var presets = await dbContext.HhSearchPresets
            .AsNoTracking()
            .OrderBy(preset => preset.Key)
            .ToListAsync(cancellationToken);

        return presets.Select(ToResponse).ToArray();
    }

    public async Task<HhSearchPresetResponse?> GetByKeyAsync(string key, CancellationToken cancellationToken)
    {
        var preset = await dbContext.HhSearchPresets
            .AsNoTracking()
            .SingleOrDefaultAsync(preset => preset.Key == key, cancellationToken);

        return preset is null ? null : ToResponse(preset);
    }

    public async Task<HhSearchPresetResponse?> CreateAsync(
        CreateHhSearchPresetRequest request,
        CancellationToken cancellationToken)
    {
        if (await dbContext.HhSearchPresets.AnyAsync(preset => preset.Key == request.Key, cancellationToken))
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var preset = new HhSearchPresetEntity
        {
            Id = Guid.NewGuid(),
            Key = request.Key,
            Label = request.Label,
            Query = request.Query,
            DefaultExclusionsJson = SerializeExclusions(request.DefaultExclusions),
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.HhSearchPresets.Add(preset);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(preset);
    }

    public async Task<HhSearchPresetResponse?> UpdateAsync(
        string key,
        UpdateHhSearchPresetRequest request,
        CancellationToken cancellationToken)
    {
        var preset = await dbContext.HhSearchPresets
            .SingleOrDefaultAsync(preset => preset.Key == key, cancellationToken);
        if (preset is null)
        {
            return null;
        }

        preset.Label = request.Label;
        preset.Query = request.Query;
        preset.DefaultExclusionsJson = SerializeExclusions(request.DefaultExclusions);
        preset.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(preset);
    }

    public async Task<bool> DeleteAsync(string key, CancellationToken cancellationToken)
    {
        var preset = await dbContext.HhSearchPresets
            .SingleOrDefaultAsync(preset => preset.Key == key, cancellationToken);
        if (preset is null)
        {
            return false;
        }

        dbContext.HhSearchPresets.Remove(preset);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static HhSearchPresetResponse ToResponse(HhSearchPresetEntity preset) => new(
        preset.Id,
        preset.Key,
        preset.Label,
        preset.Query,
        DeserializeExclusions(preset.DefaultExclusionsJson),
        preset.CreatedAt,
        preset.UpdatedAt);

    private static string? SerializeExclusions(HhJobExclusionsRequest? exclusions) =>
        exclusions is null ? null : JsonSerializer.Serialize(exclusions, JsonOptions);

    private static HhJobExclusionsRequest? DeserializeExclusions(string? json) =>
        json is null ? null : JsonSerializer.Deserialize<HhJobExclusionsRequest>(json, JsonOptions);
}
