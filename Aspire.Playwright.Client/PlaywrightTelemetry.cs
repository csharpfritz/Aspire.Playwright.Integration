using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry.Metrics;

namespace Aspire.Playwright.Client;

public static class PlaywrightTelemetry
{
	internal static readonly ActivitySource ActivitySource = new("Aspire.Hosting.Playwright");
	internal static readonly Meter Meter = new("Aspire.Hosting.Playwright");
	internal static readonly Counter<int> ConnectionAttempts = Meter.CreateCounter<int>("playwright.connection.attempts");
	internal static readonly Counter<int> ConnectionFailures = Meter.CreateCounter<int>("playwright.connection.failures");
	internal static readonly Histogram<double> ConnectionDuration = Meter.CreateHistogram<double>("playwright.connection.duration", "ms");

}