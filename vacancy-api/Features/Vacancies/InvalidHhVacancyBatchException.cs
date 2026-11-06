namespace Vacancy.Api.Features.Vacancies;

public sealed class InvalidHhVacancyBatchException(string message) : Exception(message);
