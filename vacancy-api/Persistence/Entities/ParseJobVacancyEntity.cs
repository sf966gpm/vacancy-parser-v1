namespace Vacancy.Api.Persistence.Entities;

public sealed class ParseJobVacancyEntity
{
    public Guid JobId { get; set; }
    public ParseJobEntity Job { get; set; } = null!;
    public Guid VacancyId { get; set; }
    public VacancyEntity Vacancy { get; set; } = null!;
    public DateTimeOffset FoundAt { get; set; }
}
