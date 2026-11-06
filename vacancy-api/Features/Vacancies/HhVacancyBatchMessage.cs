namespace Vacancy.Api.Features.Vacancies;

public sealed record HhVacancyBatchMessage(
    Guid JobId,
    IReadOnlyList<HhVacancyMessage> Vacancies);

public sealed record HhVacancyMessage(
    string ExternalId,
    string Title,
    string Url,
    string? Company,
    string? Location,
    string? Description,
    decimal? SalaryFrom,
    decimal? SalaryTo,
    string? SalaryCurrency,
    DateTimeOffset? PublishedAt,
    System.Text.Json.JsonElement? RawData);
