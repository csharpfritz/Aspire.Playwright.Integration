using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sample.Tests;

var builder = Host.CreateApplicationBuilder(args);

// Configure logging to include debug information
// builder.Logging.ClearProviders();
// builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Add service defaults (includes service discovery for connection strings)
builder.AddServiceDefaults();
builder.AddPlaywright("playwright");


// Register the test service
builder.Services.AddSingleton<CounterPageTests>();
builder.Services.AddSingleton<TestRunner>();

var host = builder.Build();

host.UsePlaywright();

// Run the tests
var testRunner = host.Services.GetRequiredService<TestRunner>();
await testRunner.RunTestsAsync();

// Console.WriteLine("All tests completed. Press any key to exit...");
// Console.ReadKey();
