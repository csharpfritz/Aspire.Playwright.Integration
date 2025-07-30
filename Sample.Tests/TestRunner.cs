using Microsoft.Extensions.Logging;
using System.Reflection;
using Xunit;

namespace Sample.Tests;

public class TestRunner
{
    private readonly CounterPageTests _counterPageTests;
    private readonly ILogger<TestRunner> _logger;

    public TestRunner(CounterPageTests counterPageTests, ILogger<TestRunner> logger)
    {
        _counterPageTests = counterPageTests;
        _logger = logger;
    }

    public async Task RunTestsAsync()
    {
        _logger.LogInformation("🚀 Starting Playwright end-to-end tests...");

        var testCount = 0;
        var passedCount = 0;
        var failedCount = 0;

        // Run Counter Page tests
        var result = await RunTestMethodAsync(
            "Counter Page Click Test", 
            () => _counterPageTests.CounterPage_ClickButton_IncrementsCounter());
        
        testCount += result.TestCount;
        passedCount += result.PassedCount;
        failedCount += result.FailedCount;

        // Log summary
        _logger.LogInformation("");
        _logger.LogInformation("📊 Test Summary:");
        _logger.LogInformation("   Total tests: {TotalTests}", testCount);
        _logger.LogInformation("   ✅ Passed: {PassedTests}", passedCount);
        _logger.LogInformation("   ❌ Failed: {FailedTests}", failedCount);

        if (failedCount > 0)
        {
            _logger.LogError("Some tests failed!");
            Environment.ExitCode = 1;
        }
        else
        {
            _logger.LogInformation("🎉 All tests passed!");
        }
    }

    private async Task<(int TestCount, int PassedCount, int FailedCount)> RunTestMethodAsync(
        string testName, 
        Func<Task> testMethod)
    {
        var testCount = 1;
        var passedCount = 0;
        var failedCount = 0;

        _logger.LogInformation("");
        _logger.LogInformation("🧪 Running test: {TestName}", testName);

        try
        {
            await testMethod();
            passedCount = 1;
            _logger.LogInformation("✅ {TestName} - PASSED", testName);
        }
        catch (Exception ex)
        {
            failedCount = 1;
            _logger.LogError(ex, "❌ {TestName} - FAILED: {ErrorMessage}", testName, ex.Message);
        }

        return (testCount, passedCount, failedCount);
    }
}
