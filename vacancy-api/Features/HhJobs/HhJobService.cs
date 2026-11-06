using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vacancy.Api.Persistence;
using Vacancy.Api.Persistence.Entities;

namespace Vacancy.Api.Features.HhJobs;

public sealed class HhJobService(AppDbContext dbContext) : IHhJobService
{
    private const string Platform = "hh";
    private const string OutboxMessageType = "hh.parse";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<HhParseJobAcceptedResponse?> CreateAsync(
        CreateHhParseJobRequest request,
        CancellationToken cancellationToken)
    {
        var preset = await dbContext.SearchPresets
            .AsNoTracking()
            .SingleOrDefaultAsync(
                preset => preset.Id == request.PresetId && preset.Platform == Platform,
                cancellationToken);
        if (preset is null)
        {
            return null;
        }

        var exclusions = request.Exclusions ?? DeserializeDefaultExclusions(preset.SettingsJson);
        var now = DateTimeOffset.UtcNow;
        var jobId = Guid.NewGuid();
        var job = new ParseJobEntity
        {
            Id = jobId,
            PresetId = preset.Id,
            Platform = Platform,
            PresetName = preset.Name,
            SearchQuery = preset.Query,
            ExclusionsJson = SerializeExclusions(exclusions),
            Status = "pending",
            CreatedAt = now
        };

        var wireMessage = new HhParseJobMessage(
            new HhSearchPresetRequest
            {
                Name = preset.Name,
                Query = preset.Query
            },
            exclusions);
        var outboxMessage = new OutboxMessageEntity
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            Job = job,
            Type = OutboxMessageType,
            PayloadJson = JsonSerializer.Serialize(wireMessage, JsonOptions),
            CreatedAt = now,
            AvailableAt = now
        };
        job.OutboxMessage = outboxMessage;

        dbContext.ParseJobs.Add(job);
        dbContext.OutboxMessages.Add(outboxMessage);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new HhParseJobAcceptedResponse(job.Id, job.Status, preset.Id, job.CreatedAt);
    }

    public async Task<IReadOnlyList<HhParseJobResponse>> GetRecentAsync(
        int take,
        CancellationToken cancellationToken)
    {
        var jobs = await dbContext.ParseJobs
            .AsNoTracking()
            .Where(job => job.Platform == Platform)
            .OrderByDescending(job => job.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        return jobs.Select(ToResponse).ToArray();
    }

    public async Task<HhParseJobResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var job = await dbContext.ParseJobs
            .AsNoTracking()
            .SingleOrDefaultAsync(job => job.Id == id && job.Platform == Platform, cancellationToken);

        return job is null ? null : ToResponse(job);
    }

    private static HhParseJobResponse ToResponse(ParseJobEntity job) => new(
        job.Id,
        job.PresetId,
        job.PresetName,
        job.Platform,
        job.SearchQuery,
        DeserializeExclusions(job.ExclusionsJson),
        job.Status,
        job.CreatedAt,
        job.QueuedAt,
        job.CompletedAt,
        job.FailureReason);

    private static string? SerializeExclusions(HhJobExclusionsRequest? exclusions) =>
        exclusions is null ? null : JsonSerializer.Serialize(exclusions, JsonOptions);

    private static HhJobExclusionsRequest? DeserializeExclusions(string? json) =>
        json is null ? null : JsonSerializer.Deserialize<HhJobExclusionsRequest>(json, JsonOptions);

    private static HhJobExclusionsRequest? DeserializeDefaultExclusions(string? settingsJson)
    {
        if (settingsJson is null)
        {
            return null;
        }

        using var settings = JsonDocument.Parse(settingsJson);
        return settings.RootElement.TryGetProperty("defaultExclusions", out var exclusions)
            && exclusions.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined
                ? JsonSerializer.Deserialize<HhJobExclusionsRequest>(exclusions.GetRawText(), JsonOptions)
                : null;
    }
}
