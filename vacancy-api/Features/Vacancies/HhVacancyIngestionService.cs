using Microsoft.EntityFrameworkCore;
using Vacancy.Api.Persistence;
using Vacancy.Api.Persistence.Entities;

namespace Vacancy.Api.Features.Vacancies;

public sealed class HhVacancyIngestionService(AppDbContext dbContext) : IHhVacancyIngestionService
{
    private const string Platform = "hh";

    public async Task ImportBatchAsync(HhVacancyBatchMessage batch, CancellationToken cancellationToken)
    {
        if (batch.JobId == Guid.Empty || batch.Vacancies is null || batch.Vacancies.Count == 0)
        {
            throw new InvalidHhVacancyBatchException("A vacancy batch requires a job ID and at least one vacancy.");
        }

        if (batch.Vacancies.Count > 1000)
        {
            throw new InvalidHhVacancyBatchException("A vacancy batch cannot contain more than 1000 records.");
        }

        var jobExists = await dbContext.ParseJobs
            .AnyAsync(job => job.Id == batch.JobId && job.Platform == Platform, cancellationToken);
        if (!jobExists)
        {
            throw new InvalidHhVacancyBatchException("The batch references an unknown HH parse job.");
        }

        var normalizedVacancies = batch.Vacancies
            .GroupBy(vacancy => vacancy.ExternalId, StringComparer.Ordinal)
            .Select(group => group.Last())
            .ToArray();

        if (normalizedVacancies.Any(IsInvalid))
        {
            throw new InvalidHhVacancyBatchException("Each vacancy must have an ID, title, and absolute HTTP(S) URL.");
        }

        if (normalizedVacancies.Any(vacancy =>
                vacancy.SalaryFrom < 0 || vacancy.SalaryTo < 0
                || (vacancy.SalaryFrom.HasValue && vacancy.SalaryTo.HasValue
                    && vacancy.SalaryFrom > vacancy.SalaryTo)))
        {
            throw new InvalidHhVacancyBatchException("Vacancy salary values are invalid.");
        }

        var externalIds = normalizedVacancies.Select(vacancy => vacancy.ExternalId).ToArray();
        var existingVacancies = await dbContext.Vacancies
            .Where(vacancy => vacancy.Platform == Platform && externalIds.Contains(vacancy.ExternalId))
            .ToDictionaryAsync(vacancy => vacancy.ExternalId, StringComparer.Ordinal, cancellationToken);
        var linkedExternalIds = await dbContext.ParseJobVacancies
            .Where(link => link.JobId == batch.JobId)
            .Select(link => link.Vacancy.ExternalId)
            .ToHashSetAsync(StringComparer.Ordinal, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        foreach (var message in normalizedVacancies)
        {
            if (!existingVacancies.TryGetValue(message.ExternalId, out var vacancy))
            {
                vacancy = new VacancyEntity
                {
                    Id = Guid.NewGuid(),
                    Platform = Platform,
                    ExternalId = message.ExternalId,
                    FirstSeenAt = now,
                    LastSeenAt = now,
                    Title = message.Title,
                    Url = message.Url
                };
                dbContext.Vacancies.Add(vacancy);
                existingVacancies.Add(message.ExternalId, vacancy);
            }

            vacancy.Title = message.Title;
            vacancy.Url = message.Url;
            vacancy.Company = message.Company;
            vacancy.Location = message.Location;
            vacancy.Description = message.Description;
            vacancy.SalaryFrom = message.SalaryFrom;
            vacancy.SalaryTo = message.SalaryTo;
            vacancy.SalaryCurrency = message.SalaryCurrency;
            vacancy.PublishedAt = message.PublishedAt;
            vacancy.LastSeenAt = now;
            vacancy.RawDataJson = message.RawData?.GetRawText();

            if (linkedExternalIds.Add(message.ExternalId))
            {
                dbContext.ParseJobVacancies.Add(new ParseJobVacancyEntity
                {
                    JobId = batch.JobId,
                    VacancyId = vacancy.Id,
                    FoundAt = now
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool IsInvalid(HhVacancyMessage vacancy)
    {
        if (string.IsNullOrWhiteSpace(vacancy.ExternalId)
            || string.IsNullOrWhiteSpace(vacancy.Title)
            || !Uri.TryCreate(vacancy.Url, UriKind.Absolute, out var url))
        {
            return true;
        }

        return url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps;
    }
}
