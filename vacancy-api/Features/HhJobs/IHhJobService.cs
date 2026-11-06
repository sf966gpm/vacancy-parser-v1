namespace Vacancy.Api.Features.HhJobs;

public interface IHhJobService
{
    Task<HhParseJobAcceptedResponse?> CreateAsync(
        CreateHhParseJobRequest request,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<HhParseJobResponse>> GetRecentAsync(int take, CancellationToken cancellationToken);
    Task<HhParseJobResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
