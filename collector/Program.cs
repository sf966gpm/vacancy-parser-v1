using Microsoft.Playwright;

var targetUrl = Environment.GetEnvironmentVariable("COLLECTOR_TARGET_URL") ?? "https://hh.ru/";
var userDataDir = Environment.GetEnvironmentVariable("PLAYWRIGHT_USER_DATA_DIR")
    ?? Path.Combine(AppContext.BaseDirectory, "browser-profile");

if (!Uri.TryCreate(targetUrl, UriKind.Absolute, out var uri)
    || uri.Scheme is not ("http" or "https"))
{
    Console.Error.WriteLine($"COLLECTOR_TARGET_URL must be an absolute HTTP or HTTPS URL. Value: '{targetUrl}'");
    return 1;
}

Directory.CreateDirectory(userDataDir);

Console.WriteLine($"Opening {uri} in Chromium...");
Console.WriteLine($"Browser profile: {userDataDir}");

using var playwright = await Playwright.CreateAsync();
await using var context = await playwright.Chromium.LaunchPersistentContextAsync(
    userDataDir,
    new BrowserTypeLaunchPersistentContextOptions
{
    Headless = false,
    ViewportSize = ViewportSize.NoViewport,
    Args = ["--no-sandbox", "--start-maximized", "--window-position=0,0", "--window-size=1920,1080"]
});

var page = context.Pages.FirstOrDefault() ?? await context.NewPageAsync();
var response = await page.GotoAsync(uri.ToString(), new PageGotoOptions
{
    WaitUntil = WaitUntilState.DOMContentLoaded,
    Timeout = 60_000
});

Console.WriteLine($"HTTP status: {response?.Status.ToString() ?? "no response"}");
Console.WriteLine($"Page title: {await page.TitleAsync()}");
Console.WriteLine($"Final URL: {page.Url}");
var dimensions = await page.EvaluateAsync<string>(
    "() => `${screen.width}x${screen.height} screen, ${innerWidth}x${innerHeight} viewport`");
Console.WriteLine($"Display size: {dimensions}");
Console.WriteLine("Connect to the browser at http://localhost:6080/vnc.html?autoconnect=true&resize=scale");
Console.WriteLine("The browser will stay open. Stop the container with Ctrl+C.");

await Task.Delay(Timeout.InfiniteTimeSpan);

return 0;
