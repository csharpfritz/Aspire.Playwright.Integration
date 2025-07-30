using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Aspire.Hosting.Playwright;

namespace Sample.Tests;

/// <summary>
/// Example showing advanced usage of Playwright Aspire extensions
/// with custom configuration, metrics, and error handling.
/// </summary>
public class AdvancedPlaywrightTests
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdvancedPlaywrightTests> _logger;

    public AdvancedPlaywrightTests(IConfiguration configuration, ILogger<AdvancedPlaywrightTests> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Basic usage example - simplest form
    /// </summary>
    public async Task BasicUsageExample()
    {
        using var playwright = await Playwright.CreateAsync();
        
        // Simple connection with default settings
        await using var browser = await playwright.ConnectToAspireServiceAsync(
            _configuration, 
            "playwright", 
            _logger);

        var page = await browser.NewPageAsync();
        await page.GotoAsync("https://example.com");
        
        _logger.LogInformation("Page title: {Title}", await page.TitleAsync());
    }

    /// <summary>
    /// Advanced usage with custom retry configuration
    /// </summary>
    public async Task AdvancedUsageWithCustomOptions()
    {
        using var playwright = await Playwright.CreateAsync();
        
        // Custom connection options for high-reliability scenarios
        var options = new PlaywrightConnectionOptions
        {
            MaxRetries = 10,
            InitialRetryDelay = TimeSpan.FromSeconds(5)
        };
        
        await using var browser = await playwright.ConnectToAspireServiceAsync(
            _configuration, 
            "playwright", 
            _logger,
            options);

        // The connection will retry up to 10 times with 5-second initial delay
        // and exponential backoff
        
        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            IgnoreHTTPSErrors = true,
            UserAgent = "Aspire.Playwright.Tests/1.0"
        });

        await page.GotoAsync("https://localhost:7125/counter");
        _logger.LogInformation("Successfully loaded counter page");
    }

    /// <summary>
    /// Example showing direct browser type connection
    /// </summary>
    public async Task ChromiumSpecificExample()
    {
        using var playwright = await Playwright.CreateAsync();
        
        // Connect directly to Chromium browser type
        await using var browser = await playwright.Chromium.ConnectToAspireServiceAsync(
            _configuration, 
            "playwright", 
            _logger);

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            UserAgent = "Chrome/120.0.0.0"
        });

        var page = await context.NewPageAsync();
        await page.GotoAsync("https://localhost:7125");
        
        _logger.LogInformation("Connected to Chromium specifically");
    }

    /// <summary>
    /// Example with comprehensive error handling
    /// </summary>
    public async Task ErrorHandlingExample()
    {
        using var playwright = await Playwright.CreateAsync();
        
        try
        {
            await using var browser = await playwright.ConnectToAspireServiceAsync(
                _configuration, 
                "nonexistent-playwright", // This will fail
                _logger);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("connection string"))
        {
            _logger.LogError("Playwright service not found: {Error}", ex.Message);
            throw new InvalidOperationException(
                "Playwright service 'nonexistent-playwright' is not configured. " +
                "Make sure to add .WithReference(playwright) in your AppHost configuration.", ex);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to connect"))
        {
            _logger.LogError("Playwright connection failed after retries: {Error}", ex.Message);
            throw new InvalidOperationException(
                "Unable to connect to Playwright service. Check if the service is running and accessible.", ex);
        }
    }

    /// <summary>
    /// Example showing parallel test execution with proper resource management
    /// </summary>
    public async Task ParallelTestExample()
    {
        using var playwright = await Playwright.CreateAsync();
        
        // Each task gets its own browser instance
        var tasks = Enumerable.Range(0, 3).Select(async i =>
        {
            await using var browser = await playwright.ConnectToAspireServiceAsync(
                _configuration, 
                "playwright", 
                _logger);

            var page = await browser.NewPageAsync();
            await page.GotoAsync($"https://localhost:7125/counter");
            
            // Click the counter button multiple times
            var button = page.Locator("button:has-text('Click me')");
            for (int j = 0; j < i + 1; j++)
            {
                await button.ClickAsync();
                await page.WaitForTimeoutAsync(100);
            }
            
            var counterText = await page.Locator("p:has-text('Current count:')").InnerTextAsync();
            _logger.LogInformation("Task {TaskId} final count: {Count}", i, counterText);
            
            return counterText;
        });

        var results = await Task.WhenAll(tasks);
        _logger.LogInformation("All parallel tests completed: {Results}", string.Join(", ", results));
    }

    /// <summary>
    /// Example with cancellation token usage
    /// </summary>
    public async Task CancellationExample()
    {
        using var playwright = await Playwright.CreateAsync();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // 30-second timeout
        
        try
        {
            await using var browser = await playwright.ConnectToAspireServiceAsync(
                _configuration, 
                "playwright", 
                _logger,
                options: null,
                cancellationToken: cts.Token);

            var page = await browser.NewPageAsync();
            
            // This will respect the cancellation token
            await page.GotoAsync("https://localhost:7125", new PageGotoOptions 
            { 
                Timeout = 10000 
            });
            
            _logger.LogInformation("Page loaded within timeout");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Operation was cancelled due to timeout");
            throw;
        }
    }

    /// <summary>
    /// Example showing metrics and observability integration
    /// </summary>
    public async Task ObservabilityExample()
    {
        using var playwright = await Playwright.CreateAsync();
        
        // The extension automatically creates metrics and traces
        // You can observe these in your telemetry system:
        // - playwright.connection.attempts (counter)
        // - playwright.connection.failures (counter)  
        // - playwright.connection.duration (histogram)
        // - Activity traces with "playwright.connect_to_aspire_service" name
        
        await using var browser = await playwright.ConnectToAspireServiceAsync(
            _configuration, 
            "playwright", 
            _logger);

        _logger.LogInformation("Connection metrics and traces are automatically recorded");
        
        // Your application monitoring/observability tools will show:
        // 1. How many connection attempts were made
        // 2. How many failed vs succeeded
        // 3. How long connections took
        // 4. Distributed trace spans showing the connection flow
        
        var page = await browser.NewPageAsync();
        await page.GotoAsync("https://localhost:7125");
    }
}
