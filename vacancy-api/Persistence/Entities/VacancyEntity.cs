namespace Vacancy.Api.Persistence.Entities;

public sealed class VacancyEntity
{
    public Guid Id { get; set; }
    public required string Platform { get; set; }
    public required string ExternalId { get; set; }
    public required string Title { get; set; }
    public string? Company { get; set; }
    public string? Location { get; set; }
    public required string Url { get; set; }
    public string? Description { get; set; }
    public decimal? SalaryFrom { get; set; }
    public decimal? SalaryTo { get; set; }
    public string? SalaryCurrency { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset FirstSeenAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
    public string? RawDataJson { get; set; }
    public List<ParseJobVacancyEntity> ParseJobs { get; } = [];
}
