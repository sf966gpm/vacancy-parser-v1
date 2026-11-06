using Microsoft.Playwright;

namespace Collector.Playwright.Browser;

public sealed class BrowserSession : IAsyncDisposable
{
    private const string HomeUrl = "https://hh.ru/";

    private readonly IPlaywright _playwright;
    private readonly string _userDataDir;
    private readonly SemaphoreSlim _contextLock = new(1, 1);
    private IBrowserContext _context;

    private BrowserSession(IPlaywright playwright, string userDataDir, IBrowserContext context, IPage homePage)
    {
        _playwright = playwright;
        _userDataDir = userDataDir;
        _context = context;
        HomePage = homePage;
    }

    public IPage HomePage { get; private set; }

    public static async Task<BrowserSession> StartAsync(IPlaywright playwright, string userDataDir)
    {
        Directory.CreateDirectory(userDataDir);
        var context = await LaunchContextAsync(playwright, userDataDir);
        var homePage = await GetOrCreateHomePage(context);
        return new BrowserSession(playwright, userDataDir, context, homePage);
    }

    public async Task<IPage> OpenTaskPageAsync()
    {
        await _contextLock.WaitAsync();
        try
        {
            try
            {
                await EnsureHomePageOpenAsync();
                return await _context.NewPageAsync();
            }
            catch (PlaywrightException exception)
            {
                Console.Error.WriteLine($"Browser is unavailable; reopening it before the task: {exception.Message}");
                await ReopenContextAsync();
                return await _context.NewPageAsync();
            }
        }
        finally
        {
            _contextLock.Release();
        }
    }

    public async Task<IPage> ReopenBrowserAndOpenTaskPageAsync()
    {
        await _contextLock.WaitAsync();
        try
        {
            await ReopenContextAsync();
            return await _context.NewPageAsync();
        }
        finally
        {
            _contextLock.Release();
        }
    }

    public async Task CloseTaskPageAsync(IPage page)
    {
        if (page.IsClosed)
        {
            return;
        }

        try
        {
            await page.CloseAsync();
        }
        catch (PlaywrightException)
        {
            // The browser or page may have been closed by the user during the task.
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _context.CloseAsync();
        }
        catch (PlaywrightException)
        {
            // The user may already have closed the browser.
        }

        _contextLock.Dispose();
    }

    private async Task EnsureHomePageOpenAsync()
    {
        if (!HomePage.IsClosed)
        {
            return;
        }

        HomePage = await _context.NewPageAsync();
        await HomePage.GotoAsync(HomeUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 60_000
        });
    }

    private async Task ReopenContextAsync()
    {
        try
        {
            await _context.CloseAsync();
        }
        catch (PlaywrightException)
        {
            // Closing an already-closed persistent context can throw.
        }

        ClearStaleChromiumProfileLocks();
        _context = await LaunchContextAsync(_playwright, _userDataDir);
        HomePage = await GetOrCreateHomePage(_context);
        await HomePage.GotoAsync(HomeUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 60_000
        });
    }

    private static async Task<IBrowserContext> LaunchContextAsync(IPlaywright playwright, string userDataDir)
    {
        return await playwright.Chromium.LaunchPersistentContextAsync(
            userDataDir,
            new BrowserTypeLaunchPersistentContextOptions
            {
                Headless = false,
                ViewportSize = ViewportSize.NoViewport,
                Args = ["--no-sandbox", "--start-maximized", "--window-position=0,0", "--window-size=1920,1080"]
            });
    }

    private static async Task<IPage> GetOrCreateHomePage(IBrowserContext context)
    {
        return context.Pages.FirstOrDefault() ?? await context.NewPageAsync();
    }

    private void ClearStaleChromiumProfileLocks()
    {
        foreach (var lockName in new[] { "SingletonLock", "SingletonCookie", "SingletonSocket" })
        {
            var lockPath = Path.Combine(_userDataDir, lockName);
            if (File.Exists(lockPath) || new FileInfo(lockPath).LinkTarget is not null)
            {
                Console.WriteLine($"Removing stale Chromium profile lock: {lockPath}");
                File.Delete(lockPath);
            }
        }
    }
}
