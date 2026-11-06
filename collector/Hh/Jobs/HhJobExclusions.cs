namespace Collector.Playwright.Hh.Jobs;

public sealed record HhJobExclusions(
    IReadOnlyList<long>? VacancyIds,
    IReadOnlyList<string>? TitleCategories);
