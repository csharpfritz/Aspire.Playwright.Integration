using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace Sample.Tests;

public class CounterPageTests
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<CounterPageTests> _logger;

    public CounterPageTests(IConfiguration configuration, ILogger<CounterPageTests> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task CounterPage_ClickButton_IncrementsCounter()
    {

        // Create Playwright instance and connect to remote server using Aspire service discovery
        using var playwright = await Playwright.CreateAsync();
        
        // Use the extension method to connect with built-in retry logic and metrics
        await using var browser = await playwright.ConnectToPlaywrightServiceAsync();

        _logger.LogInformation("Connected to Playwright browser");

        // Create a new page with options to ignore SSL certificate errors
        // This is necessary when connecting from container to host machine with development certificates
        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            IgnoreHTTPSErrors = true
        });

        try
        {
            // Navigate to the Counter page
         
            await page.GotoAspireResourcePageAsync("webfrontend", "/counter", new PageGotoOptions { Timeout = 15000 });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 15000 });

            _logger.LogInformation("Page loaded successfully");

            // Verify we're on the counter page
            var pageTitle = await page.TitleAsync();
            _logger.LogInformation("Page title: {PageTitle}", pageTitle);

            // Find the counter display element and get initial value
            var counterElement = page.Locator("p").Filter(new() { HasText = "Current count:" });
            await counterElement.WaitForAsync();

            var initialCountText = await counterElement.InnerTextAsync();
            _logger.LogInformation("Initial counter text: {InitialCount}", initialCountText);

            // Extract the initial count number
            var initialCount = ExtractCountFromText(initialCountText);
            _logger.LogInformation("Initial count value: {InitialCountValue}", initialCount);

            // Find and click the "Click me" button
            var clickButton = page.Locator("button").Filter(new() { HasText = "Click me" });
            await clickButton.WaitForAsync();

            _logger.LogInformation("Clicking the button...");
            await clickButton.ClickAsync();

            // Wait a moment for the counter to update
            await page.WaitForTimeoutAsync(500);

            // Get the updated counter value
            var updatedCountText = await counterElement.InnerTextAsync();
            var updatedCount = ExtractCountFromText(updatedCountText);

            _logger.LogInformation("Updated counter text: {UpdatedCount}", updatedCountText);
            _logger.LogInformation("Updated count value: {UpdatedCountValue}", updatedCount);

            // Assert that the counter incremented by 1
            if (updatedCount != initialCount + 1)
            {
                throw new InvalidOperationException($"Counter test failed! Expected {initialCount + 1}, but got {updatedCount}");
            }

            _logger.LogInformation("✅ Counter test passed! Counter incremented from {Initial} to {Updated}", 
                initialCount, updatedCount);

            // Click the button a few more times to make sure it keeps working
            for (int i = 0; i < 3; i++)
            {
                await clickButton.ClickAsync();
                await page.WaitForTimeoutAsync(200);
            }

            var finalCountText = await counterElement.InnerTextAsync();
            var finalCount = ExtractCountFromText(finalCountText);

            _logger.LogInformation("After clicking 3 more times, final count: {FinalCount}", finalCount);
            if (finalCount != initialCount + 4)
            {
                throw new InvalidOperationException($"Multiple clicks test failed! Expected {initialCount + 4}, but got {finalCount}");
            }

            _logger.LogInformation("✅ All counter tests passed!");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private static int ExtractCountFromText(string text)
    {
        // Extract number from text like "Current count: 5"
        var parts = text.Split(':');
        if (parts.Length >= 2 && int.TryParse(parts[1].Trim(), out var count))
        {
            return count;
        }
        throw new InvalidOperationException($"Could not extract count from text: {text}");
    }
}
