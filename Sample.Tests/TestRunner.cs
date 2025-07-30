using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Xunit.Runners;

namespace Sample.Tests;

/// <summary>
/// Program entry point for running tests programmatically using xUnit's programmatic runner.
/// This allows running all xUnit tests when the console application is launched.
/// </summary>
public class TestRunner
{
	private static bool _testsFailed = false;
	private static int _totalTests = 0;
	private static int _passedTests = 0;
	private static int _failedTests = 0;
	private static int _skippedTests = 0;

	public static async Task Main(string[] args)
	{
		// Build host for dependency injection - but don't try to connect to Playwright yet
		var builder = Host.CreateApplicationBuilder(args);

		// Only add Playwright if the connection string is available
		var configuration = builder.Configuration;
		var playwrightConnectionString = configuration.GetConnectionString("playwright");
		if (!string.IsNullOrEmpty(playwrightConnectionString))
		{
			builder.AddPlaywright("playwright");
		}
		builder.AddServiceDefaults();

		var host = builder.Build();

		// Only initialize Playwright if it was added
		if (!string.IsNullOrEmpty(playwrightConnectionString))
		{
			host.UsePlaywright();
		}

		var logger = host.Services.GetRequiredService<ILogger<TestRunner>>();

		logger.LogInformation("🎬 Starting Playwright end-to-end tests programmatically...");

		// Log OpenTelemetry configuration for debugging
		var otelEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
		if (!string.IsNullOrEmpty(otelEndpoint))
		{
			logger.LogInformation("📊 OpenTelemetry OTLP endpoint detected: {Endpoint}", otelEndpoint);
		}
		else
		{
			logger.LogWarning("⚠️ No OTEL_EXPORTER_OTLP_ENDPOINT found - metrics will use console exporter only");
		}

		try
		{
			// Get the current assembly containing the tests
			var testAssembly = Assembly.GetExecutingAssembly();

			logger.LogInformation("🏃 Running xUnit tests from assembly: {AssemblyName}", testAssembly.GetName().Name);

			// Create and configure the xUnit test runner
			using var runner = AssemblyRunner.WithoutAppDomain(testAssembly.Location);

			// Set up event handlers for test results
			runner.OnDiscoveryComplete = OnDiscoveryComplete;
			runner.OnExecutionComplete = OnExecutionComplete;
			runner.OnTestFailed = OnTestFailed;
			runner.OnTestPassed = OnTestPassed;
			runner.OnTestSkipped = OnTestSkipped;

			// Start the test run
			runner.Start();

			// Wait for completion
			var completionSource = new TaskCompletionSource<bool>();
			runner.OnExecutionComplete = info =>
			{
				OnExecutionComplete(info);
				completionSource.SetResult(true);
			};

			await completionSource.Task;

			// Log final results
			logger.LogInformation("📊 Test Results Summary:");
			logger.LogInformation("   Total: {Total}", _totalTests);
			logger.LogInformation("   ✅ Passed: {Passed}", _passedTests);
			logger.LogInformation("   ❌ Failed: {Failed}", _failedTests);
			logger.LogInformation("   ⏭️ Skipped: {Skipped}", _skippedTests);

			if (_testsFailed)
			{
				logger.LogError("❌ Some tests failed!");
				Environment.Exit(1);
			}
			else
			{
				logger.LogInformation("✅ All tests passed successfully!");
				Environment.Exit(0);
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "💥 Error running tests");
			Environment.Exit(1);
		}
	}

	private static void OnDiscoveryComplete(DiscoveryCompleteInfo info)
	{
		Console.WriteLine($"🔍 Discovery complete: Found {info.TestCasesToRun} tests to run");
		_totalTests = info.TestCasesToRun;
	}

	private static void OnExecutionComplete(ExecutionCompleteInfo info)
	{
		Console.WriteLine($"� Execution complete: {info.TotalTests} total, {info.TestsFailed} failed, {info.TestsSkipped} skipped");
		_testsFailed = info.TestsFailed > 0;
	}

	private static void OnTestPassed(TestPassedInfo info)
	{
		_passedTests++;
		Console.WriteLine($"✅ PASS: {info.TestDisplayName} ({info.ExecutionTime:F3}s)");
	}

	private static void OnTestFailed(TestFailedInfo info)
	{
		_failedTests++;
		_testsFailed = true;
		Console.WriteLine($"❌ FAIL: {info.TestDisplayName} ({info.ExecutionTime:F3}s)");
		if (!string.IsNullOrEmpty(info.ExceptionMessage))
		{
			Console.WriteLine($"   Error: {info.ExceptionMessage}");
		}
		if (!string.IsNullOrEmpty(info.ExceptionStackTrace))
		{
			Console.WriteLine($"   Stack Trace: {info.ExceptionStackTrace}");
		}
	}

	private static void OnTestSkipped(TestSkippedInfo info)
	{
		_skippedTests++;
		Console.WriteLine($"⏭️ SKIP: {info.TestDisplayName}");
		if (!string.IsNullOrEmpty(info.SkipReason))
		{
			Console.WriteLine($"   Reason: {info.SkipReason}");
		}
	}
}
