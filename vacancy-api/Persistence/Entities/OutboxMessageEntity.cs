namespace Vacancy.Api.Persistence.Entities;

public sealed class OutboxMessageEntity
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public ParseJobEntity Job { get; set; } = null!;
    public required string Type { get; set; }
    public required string PayloadJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset AvailableAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}
