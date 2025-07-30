# Playwright Extension Usage Examples

This document shows how to use the Aspire Playwright extension methods for easier integration with Aspire service discovery.

## Basic Usage

The simplest way to connect to a Playwright service in your Aspire application:

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Aspire.Hosting.Playwright;

public class MyTests
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MyTests> _logger;

    public MyTests(IConfiguration configuration, ILogger<MyTests> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task RunTest()
    {
        // Create Playwright instance
        using var playwright = await Playwright.CreateAsync();
        
        // Connect using Aspire service discovery - automatically handles retries and logging
        await using var browser = await playwright.ConnectToAspireServiceAsync(
            _configuration, 
            "playwright", // Name of the Playwright resource from your AppHost
            _logger);

        // Create a page and run your tests
        var page = await browser.NewPageAsync();
        await page.GotoAsync("https://example.com");
        
        // Your test logic here...
    }
}
```

## Advanced Usage with Custom Options

For more control over connection behavior:

```csharp
using Aspire.Hosting.Playwright;

public async Task RunTestWithCustomOptions()
{
    using var playwright = await Playwright.CreateAsync();
    
    // Custom connection options
    var options = new PlaywrightConnectionOptions
    {
        MaxRetries = 10,
        InitialRetryDelay = TimeSpan.FromSeconds(5)
    };
    
    // Connect with custom options
    await using var browser = await playwright.ConnectToAspireServiceAsync(
        _configuration, 
        "playwright", 
        _logger,
        options);

    // Your test logic...
}
```

## Specific Browser Types

You can also connect directly to specific browser types:

```csharp
public async Task RunChromeSpecificTest()
{
    using var playwright = await Playwright.CreateAsync();
    
    // Connect specifically to Chromium
    await using var browser = await playwright.Chromium.ConnectToAspireServiceAsync(
        _configuration, 
        "playwright", 
        _logger);

    // Your Chrome-specific test logic...
}
```

## AppHost Configuration

In your `AppHost.cs` file, configure the Playwright service:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add Playwright service with a specific name
var playwright = builder.AddPlaywright("playwright")
    .WithDataVolume()  // Optional: Add persistent volume for browser cache
    .WithDevelopmentMode(); // Optional: Add development capabilities

// Add your test project with reference to Playwright
var tests = builder.AddProject<Projects.MyTests>("tests")
    .WithReference(playwright)
    .WaitFor(playwright);

builder.Build().Run();
```

## Features

### Built-in Retry Logic
- Automatic retry with exponential backoff
- Configurable retry attempts and delays
- Detailed logging of retry attempts

### Metrics and Observability
- Connection attempt counters
- Connection failure counters  
- Connection duration histograms
- Distributed tracing with activities

### Service Discovery Integration
- Automatic connection string resolution from Aspire configuration
- No need to manually handle URLs or ports
- Works seamlessly with Aspire's service discovery

### Logging Integration
- Structured logging with correlation IDs
- Debug information for troubleshooting
- Connection status and timing information

## Error Handling

The extension methods provide clear error messages and throw `InvalidOperationException` in these cases:

1. **Missing Connection String**: When the Playwright service is not properly referenced in the AppHost
2. **Connection Failures**: When all retry attempts are exhausted
3. **Invalid Configuration**: When required parameters are null or invalid

## Migration from Manual Connection

### Before (Manual):
```csharp
var playwrightUrl = _configuration.GetConnectionString("playwright");
if (string.IsNullOrEmpty(playwrightUrl))
    throw new InvalidOperationException("Playwright connection string not found");

IBrowser? browser = null;
for (int attempt = 1; attempt <= 5; attempt++)
{
    try
    {
        browser = await playwright.Chromium.ConnectAsync(playwrightUrl);
        break;
    }
    catch (Exception ex) when (attempt < 5)
    {
        _logger.LogWarning("Retry {Attempt}: {Error}", attempt, ex.Message);
        await Task.Delay(TimeSpan.FromSeconds(2));
    }
}
```

### After (With Extension):
```csharp
await using var browser = await playwright.ConnectToAspireServiceAsync(
    _configuration, 
    "playwright", 
    _logger);
```

The extension method handles all the complexity of connection string resolution, retry logic, error handling, and logging automatically.
