namespace Vacancy.Api.Persistence.Entities;

public sealed class ParseJobEntity
{
    public Guid Id { get; set; }
    public Guid? PresetId { get; set; }
    public HhSearchPresetEntity? Preset { get; set; }
    public required string Platform { get; set; }
    public required string PresetKey { get; set; }
    public required string PresetLabel { get; set; }
    public required string SearchQuery { get; set; }
    public string? ExclusionsJson { get; set; }
    public required string Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? QueuedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? FailureReason { get; set; }
    public OutboxMessageEntity? OutboxMessage { get; set; }
    public List<ParseJobVacancyEntity> JobVacancies { get; } = [];
}
