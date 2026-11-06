namespace Vacancy.Api.Features.Presets;

public interface ISearchPresetService
{
    Task<IReadOnlyList<SearchPresetResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<SearchPresetResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<SearchPresetResponse> CreateAsync(CreateSearchPresetRequest request, CancellationToken cancellationToken);
    Task<SearchPresetResponse?> UpdateAsync(Guid id, UpdateSearchPresetRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
