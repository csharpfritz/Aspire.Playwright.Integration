using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;

namespace Sample.Tests;

/// <summary>
/// Program entry point for running tests programmatically using dotnet test.
/// This allows running all xUnit tests when the console application is launched.
/// </summary>
public class TestRunner
{
    public static async Task Main(string[] args)
    {
        // Build host for dependency injection
        var builder = Host.CreateApplicationBuilder(args);
        builder.AddServiceDefaults();
        builder.AddPlaywright("playwright");
        
        var host = builder.Build();
        host.UsePlaywright();

        var logger = host.Services.GetRequiredService<ILogger<TestRunner>>();
        
        logger.LogInformation("🎬 Starting Playwright end-to-end tests programmatically...");
        
        try
        {
            // Get the directory of the current assembly (test project)
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var projectDirectory = Path.GetDirectoryName(assemblyLocation);
            
            // Find the .csproj file in the project directory or parent directories
            var csprojPath = FindProjectFile(projectDirectory!);
            
            if (csprojPath == null)
            {
                logger.LogError("❌ Could not find .csproj file to run tests");
                Environment.Exit(1);
                return;
            }

            // Run tests using dotnet test command
            var testResults = await RunDotnetTestAsync(csprojPath, logger);
            
            if (testResults.ExitCode != 0)
            {
                logger.LogError("❌ Tests failed with exit code {ExitCode}", testResults.ExitCode);
                Environment.Exit(testResults.ExitCode);
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

    private static string? FindProjectFile(string startDirectory)
    {
        var currentDir = new DirectoryInfo(startDirectory);
        
        while (currentDir != null)
        {
            var csprojFiles = currentDir.GetFiles("*.csproj");
            if (csprojFiles.Length > 0)
            {
                return csprojFiles[0].FullName;
            }
            
            currentDir = currentDir.Parent;
        }
        
        return null;
    }

    private static async Task<TestResults> RunDotnetTestAsync(string projectPath, ILogger logger)
    {
        logger.LogInformation("🏃 Running dotnet test on project: {ProjectPath}", projectPath);
        
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"test \"{projectPath}\" --logger \"console;verbosity=detailed\" --no-build --no-restore",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        var process = new Process { StartInfo = processStartInfo };
        var outputLines = new List<string>();
        var errorLines = new List<string>();

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                outputLines.Add(e.Data);
                logger.LogInformation("📄 {Output}", e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                errorLines.Add(e.Data);
                logger.LogError("❗ {Error}", e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        return new TestResults
        {
            ExitCode = process.ExitCode,
            Output = outputLines,
            Errors = errorLines
        };
    }

    private class TestResults
    {
        public int ExitCode { get; set; }
        public List<string> Output { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }
}
