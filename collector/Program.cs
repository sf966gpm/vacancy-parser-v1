using Microsoft.Playwright;
using Collector.Playwright.Browser;
using Collector.Playwright.Hh.Jobs;

const string targetUrl = "https://hh.ru/";
var userDataDir = Environment.GetEnvironmentVariable("PLAYWRIGHT_USER_DATA_DIR")
    ?? Path.Combine(AppContext.BaseDirectory, "browser-profile");

var uri = new Uri(targetUrl);

Directory.CreateDirectory(userDataDir);

Console.WriteLine($"Opening {uri} in Chromium...");
Console.WriteLine($"Browser profile: {userDataDir}");

using var playwright = await Playwright.CreateAsync();
await using var browserSession = await BrowserSession.StartAsync(playwright, userDataDir);
var page = browserSession.HomePage;
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

await using var jobConsumer = await HhJobConsumer.ConnectAsync(browserSession);
Console.WriteLine("Waiting for parse jobs from RabbitMQ.");
await Task.Delay(Timeout.InfiniteTimeSpan);

return 0;
