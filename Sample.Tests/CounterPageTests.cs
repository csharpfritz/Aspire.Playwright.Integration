using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace Sample.Tests;

/// <summary>
/// Test collection to ensure PlaywrightFixture is shared across all tests.
/// This ensures only one browser connection is created for all tests.
/// </summary>
[Collection("Playwright")]
public class CounterPageTests : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _fixture;
    private readonly ILogger<CounterPageTests> _logger;
    private readonly ITestOutputHelper _output;

    public CounterPageTests(PlaywrightFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _logger = fixture.LoggerFactory.CreateLogger<CounterPageTests>();
        _output = output;
    }

    [Fact]
    public async Task CounterPage_ClickButton_IncrementsCounter()
    {
        // Arrange
        var page = await _fixture.CreatePageAsync();
        _output.WriteLine("🧪 Starting counter increment test...");

        try
        {
            // Act - Navigate to the Counter page
            await page.GotoAspireResourcePageAsync("webfrontend", "/counter", new PageGotoOptions { Timeout = 15000 });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 15000 });

            _logger.LogInformation("Page loaded successfully");
            _output.WriteLine("✅ Page loaded successfully");

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

            // Assert - Use proper xUnit assertions
            Assert.Equal(initialCount + 1, updatedCount);

            _logger.LogInformation("✅ Counter test passed! Counter incremented from {Initial} to {Updated}", 
                initialCount, updatedCount);
            _output.WriteLine($"✅ Counter incremented from {initialCount} to {updatedCount}");

            // Click the button a few more times to make sure it keeps working
            for (int i = 0; i < 3; i++)
            {
                await clickButton.ClickAsync();
                await page.WaitForTimeoutAsync(200);
            }

            var finalCountText = await counterElement.InnerTextAsync();
            var finalCount = ExtractCountFromText(finalCountText);

            _logger.LogInformation("After clicking 3 more times, final count: {FinalCount}", finalCount);
            
            // Assert final count is correct
            Assert.Equal(initialCount + 4, finalCount);

            _logger.LogInformation("✅ All counter tests passed!");
            _output.WriteLine($"✅ Final count after multiple clicks: {finalCount}");
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
