// Example usage in a test project or service

using Microsoft.Playwright;

public class PlaywrightService
{
    private readonly string _playwrightUrl;

    public PlaywrightService(IConfiguration configuration)
    {
        // Get the Playwright connection string from Aspire configuration
        _playwrightUrl = configuration.GetConnectionString("playwright") 
            ?? throw new InvalidOperationException("Playwright connection string not found");
    }

    public async Task<string> ScrapePageContentAsync(string url)
    {
        // Create Playwright instance and connect to remote server
        using var playwright = await Playwright.CreateAsync();
        
        // Connect to the Playwright server running in Docker
        await using var browser = await playwright.Chromium.ConnectAsync(_playwrightUrl);
        
        // Create a new page and navigate
        var page = await browser.NewPageAsync();
        await page.GotoAsync(url);
        
        // Extract content
        var content = await page.InnerTextAsync("body");
        
        return content;
    }

    public async Task RunEndToEndTestAsync()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.ConnectAsync(_playwrightUrl);
        
        var page = await browser.NewPageAsync();
        
        // Test against local services using 'hostmachine' hostname
        // (if WithHostNetworkAccess() was used in AppHost)
        await page.GotoAsync("http://hostmachine:5000/");
        
        // Perform test actions
        await page.ClickAsync("text=Login");
        await page.FillAsync("#username", "testuser");
        await page.FillAsync("#password", "testpass");
        await page.ClickAsync("input[type=submit]");
        
        // Assert results
        await page.WaitForSelectorAsync("text=Welcome");
    }
}

// Example DI registration in Program.cs
public static void Main(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);
    
    // Add service defaults (includes service discovery)
    builder.AddServiceDefaults();
    
    // Register your service that uses Playwright
    builder.Services.AddSingleton<PlaywrightService>();
    
    var app = builder.Build();
    
    // Map endpoints
    app.MapGet("/scrape", async (PlaywrightService service, string url) =>
    {
        var content = await service.ScrapePageContentAsync(url);
        return Results.Ok(new { content });
    });
    
    app.MapDefaultEndpoints();
    app.Run();
}
