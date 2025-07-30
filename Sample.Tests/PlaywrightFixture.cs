using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Xunit;

namespace Sample.Tests;

/// <summary>
/// xUnit fixture that manages the Playwright connection lifecycle for tests.
/// Implements IAsyncLifetime to properly initialize and dispose of resources.
/// </summary>
public class PlaywrightFixture : IAsyncLifetime
{
    private IHost? _host;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public IConfiguration Configuration { get; private set; } = null!;
    public ILoggerFactory LoggerFactory { get; private set; } = null!;
    public ILogger<PlaywrightFixture> Logger { get; private set; } = null!;
    public IBrowser Browser => _browser ?? throw new InvalidOperationException("Browser not initialized. Call InitializeAsync first.");

    /// <summary>
    /// Initialize the Playwright services and connect to the browser.
    /// Called once before all tests in the collection.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Build the host with necessary services
        var builder = Host.CreateApplicationBuilder();
        
        // Configure logging
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
        
        // Add service defaults (includes service discovery for connection strings)
        builder.AddServiceDefaults();
        builder.AddPlaywright("playwright");

        _host = builder.Build();
        _host.UsePlaywright();

        // Get services from DI container
        Configuration = _host.Services.GetRequiredService<IConfiguration>();
        LoggerFactory = _host.Services.GetRequiredService<ILoggerFactory>();
        Logger = LoggerFactory.CreateLogger<PlaywrightFixture>();

        Logger.LogInformation("🚀 Initializing Playwright fixture...");

        // Create Playwright instance and connect to remote server using Aspire service discovery
        _playwright = await Playwright.CreateAsync();
        
        // Use the extension method to connect with built-in retry logic and metrics
        _browser = await _playwright.ConnectToPlaywrightServiceAsync();

        Logger.LogInformation("✅ Playwright fixture initialized successfully");
    }

    /// <summary>
    /// Create a new page with default options for testing.
    /// </summary>
    /// <returns>A new page instance with SSL errors ignored.</returns>
    public async Task<IPage> CreatePageAsync()
    {
        var page = await Browser.NewPageAsync(new BrowserNewPageOptions
        {
            // This is necessary when connecting from container to host machine with development certificates
            IgnoreHTTPSErrors = true
        });

        return page;
    }

    /// <summary>
    /// Clean up resources when all tests are complete.
    /// Called once after all tests in the collection.
    /// </summary>
    public async Task DisposeAsync()
    {
        Logger?.LogInformation("🧹 Disposing Playwright fixture...");

        if (_browser != null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        _host?.Dispose();

        Logger?.LogInformation("✅ Playwright fixture disposed");
    }
}
