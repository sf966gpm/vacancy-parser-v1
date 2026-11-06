namespace Vacancy.Api.Features.HhPresets;

public interface IHhSearchPresetService
{
    Task<IReadOnlyList<HhSearchPresetResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<HhSearchPresetResponse?> GetByKeyAsync(string key, CancellationToken cancellationToken);
    Task<HhSearchPresetResponse?> CreateAsync(
        CreateHhSearchPresetRequest request,
        CancellationToken cancellationToken);
    Task<HhSearchPresetResponse?> UpdateAsync(
        string key,
        UpdateHhSearchPresetRequest request,
        CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string key, CancellationToken cancellationToken);
}
