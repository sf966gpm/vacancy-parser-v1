namespace Vacancy.Api.Features.Vacancies;

public interface IHhVacancyIngestionService
{
    Task ImportBatchAsync(HhVacancyBatchMessage batch, CancellationToken cancellationToken);
}
