namespace Vacancy.Api.Persistence.Entities;

public sealed class HhSearchPresetEntity
{
    public Guid Id { get; set; }
    public required string Key { get; set; }
    public required string Label { get; set; }
    public required string Query { get; set; }
    public string? DefaultExclusionsJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public List<ParseJobEntity> Jobs { get; } = [];
}
