namespace Collector.Playwright.Hh.Jobs;

public sealed record HhParseJobMessage(
    HhSearchPreset? Preset,
    HhJobExclusions? Exclusions);
