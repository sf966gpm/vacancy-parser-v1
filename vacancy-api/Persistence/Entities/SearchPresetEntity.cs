namespace Vacancy.Api.Persistence.Entities;

public sealed class SearchPresetEntity
{
    public Guid Id { get; set; }
    public required string Platform { get; set; }
    public required string Name { get; set; }
    public required string Query { get; set; }
    public string? SettingsJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public List<ParseJobEntity> Jobs { get; } = [];
}
