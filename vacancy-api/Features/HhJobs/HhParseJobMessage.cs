namespace Vacancy.Api.Features.HhJobs;

public sealed record HhParseJobMessage(
    HhSearchPresetRequest Preset,
    HhJobExclusionsRequest? Exclusions);
