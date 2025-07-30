using Aspire.Hosting.Playwright;
using Aspire.Playwright.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Playwright;
using System.Diagnostics;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for connecting to Playwright services through Aspire service discovery.
/// </summary>
public static class PlaywrightClientExtensions
{


	// we need an AddPlaywright method that adds the Playwright telemetry
	public static IHostApplicationBuilder AddPlaywright(this IHostApplicationBuilder builder, string playwrightServiceName, Action<PlaywrightConnectionOptions>? configureOptions = null)
	{

		// Retrieve the Playwright service URL from configuration
		ArgumentException.ThrowIfNullOrWhiteSpace(playwrightServiceName, nameof(playwrightServiceName));
		var playwrightUrl = builder.Configuration.GetConnectionString(playwrightServiceName);
		if (string.IsNullOrEmpty(playwrightUrl))
		{
			throw new InvalidOperationException($"Playwright connection string '{playwrightServiceName}' not found. Make sure the Playwright service is registered in Aspire.");
		}

		PlaywrightService.Instance.Configuration = builder.Configuration;

		builder.Services.AddOpenTelemetry()
						.WithMetrics(metrics =>
						{
							metrics.AddMeter(PlaywrightTelemetry.Meter.Name);
						})
						.WithTracing(tracing =>
						{
							tracing.AddSource(PlaywrightTelemetry.ActivitySource.Name);
						});
		//builder.Services.AddOpenTelemetry().UseOtlpExporter();

		var options = new PlaywrightConnectionOptions
		{
			ServiceUri = new Uri(playwrightUrl),
			PlaywrightServiceResourceName = playwrightServiceName
		};

		configureOptions?.Invoke(options);
		PlaywrightService.Instance.Configure(options, NullLogger<PlaywrightService>.Instance);

		return builder;
	}

	/// <summary>
	/// Adds Playwright client services to the application builder.
	/// This allows the application to connect to a Playwright service registered in Aspire.
	/// </summary>
	/// <param name="builder">The application builder.</param>
	/// <param name="playwrightResourceName">The name of the Playwright resource registered in Aspire.</param>
	public static IHost UsePlaywright(this IHost host)
	{
		ArgumentNullException.ThrowIfNull(host, nameof(host));

		var logger = host.Services.GetRequiredService<ILogger<PlaywrightService>>();

		PlaywrightService.Instance.Configure(PlaywrightService.Instance.Options, logger);

		return host;

	}



	/// <summary>
	/// Connects to a Playwright service using Aspire service discovery.
	/// </summary>
	/// <param name="playwright">The Playwright instance.</param>
	/// <param name="configuration">The configuration to retrieve connection strings from.</param>
	/// <param name="resourceName">The name of the Playwright resource in Aspire.</param>
	/// <param name="logger">Optional logger for diagnostic information.</param>
	/// <param name="options">Optional connection options. Uses defaults if not provided.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A connected browser instance.</returns>
	/// <exception cref="InvalidOperationException">Thrown when the connection string is not found or connection fails.</exception>
	public static async Task<IBrowser> ConnectToPlaywrightServiceAsync(
			this IPlaywright playwright,
			CancellationToken cancellationToken = default)
	{
		return await playwright.Chromium.ConnectToPlaywrightServiceAsync(
				PlaywrightService.Instance.Options, cancellationToken);
	}

	/// <summary>
	/// Connects to a Playwright Chromium service using Aspire service discovery.
	/// </summary>
	/// <param name="chromium">The Chromium browser type.</param>
	/// <param name="configuration">The configuration to retrieve connection strings from.</param>
	/// <param name="resourceName">The name of the Playwright resource in Aspire.</param>
	/// <param name="logger">Optional logger for diagnostic information.</param>
	/// <param name="options">Optional connection options. Uses defaults if not provided.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A connected browser instance.</returns>
	/// <exception cref="InvalidOperationException">Thrown when the connection string is not found or connection fails.</exception>
	public static async Task<IBrowser> ConnectToPlaywrightServiceAsync(
			this IBrowserType chromium,
			PlaywrightConnectionOptions? options = null,
			CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(chromium);

		options ??= PlaywrightConnectionOptions.Default;
		var retryDelay = options.InitialRetryDelay;

		using var activity = PlaywrightTelemetry.ActivitySource.StartActivity("playwright.connect_to_playwright_service");
		activity?.SetTag("resource.name", options.PlaywrightServiceResourceName);
		activity?.SetTag("max.retries", options.MaxRetries);

		var stopwatch = Stopwatch.StartNew();

		try
		{

			PlaywrightService.Instance.Logger?.LogInformation("Connecting to Playwright service '{ResourceName}' at {PlaywrightUrl}", options.PlaywrightServiceResourceName, options.ServiceUri);
			activity?.SetTag("playwright.url", options.ServiceUri);

			IBrowser? browser = null;
			Exception? lastException = null;

			for (int attempt = 1; attempt <= options.MaxRetries; attempt++)
			{
				try
				{
					PlaywrightTelemetry.ConnectionAttempts.Add(1, new KeyValuePair<string, object?>("resource.name", options.PlaywrightServiceResourceName));

					PlaywrightService.Instance.Logger?.LogDebug("Attempting to connect to Playwright (attempt {Attempt}/{MaxRetries})", attempt, options.MaxRetries);

					browser = await chromium.ConnectAsync(PlaywrightService.Instance.PlaywrightServiceResource!.ToString(), new BrowserTypeConnectOptions
					{
						Timeout = (int)retryDelay.TotalMilliseconds
					});

					PlaywrightService.Instance.Logger?.LogInformation("Successfully connected to Playwright service '{ResourceName}' on attempt {Attempt}", options.PlaywrightServiceResourceName, attempt);
					activity?.SetTag("connection.attempt", attempt);

					break;
				}
				catch (Exception ex) when (attempt < options.MaxRetries && !cancellationToken.IsCancellationRequested)
				{
					lastException = ex;
					PlaywrightTelemetry.ConnectionFailures.Add(1, new KeyValuePair<string, object?>("resource.name", options.PlaywrightServiceResourceName));

					PlaywrightService.Instance.Logger?.LogWarning("Failed to connect to Playwright service '{ResourceName}' on attempt {Attempt}: {Error}. Retrying in {Delay}...",
							options.PlaywrightServiceResourceName, attempt, ex.Message, retryDelay);

					await Task.Delay(retryDelay, cancellationToken);
					retryDelay = TimeSpan.FromMilliseconds(retryDelay.TotalMilliseconds * 1.5); // Exponential backoff
				}
			}

			if (browser == null)
			{
				var errorMessage = $"Failed to connect to Playwright service '{options.PlaywrightServiceResourceName}' after {options.MaxRetries} attempts. Last error: {lastException?.Message}";
				PlaywrightService.Instance.Logger?.LogError("Playwright connection failed: {Error}", errorMessage);
				activity?.SetStatus(ActivityStatusCode.Error, errorMessage);
				PlaywrightTelemetry.ConnectionFailures.Add(1, new KeyValuePair<string, object?>("resource.name", options.PlaywrightServiceResourceName));
				throw new InvalidOperationException(errorMessage, lastException);
			}

			PlaywrightTelemetry.ConnectionDuration.Record(stopwatch.ElapsedMilliseconds, new KeyValuePair<string, object?>("resource.name", options.PlaywrightServiceResourceName));
			activity?.SetStatus(ActivityStatusCode.Ok);

			return browser;
		}
		catch (Exception ex)
		{
			activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
			throw;
		}
		finally
		{
			stopwatch.Stop();
		}
	}

	public static Task<IResponse?> GotoAspireResourcePageAsync(this IPage page,
		string aspireResourceName,
		string relativeUrl = "",
		PageGotoOptions? options = null)
	{
		ArgumentNullException.ThrowIfNull(page);
		ArgumentException.ThrowIfNullOrWhiteSpace(aspireResourceName, nameof(aspireResourceName));

		var playwrightService = PlaywrightService.Instance;
		if (!playwrightService.IsConfigured)
		{
			throw new InvalidOperationException("Playwright service is not configured. Please call UsePlaywright() on the host.");
		}

		try
		{
			Uri resourceUrl = playwrightService.ResolveResourceUrl(aspireResourceName);

			// Normalize and validate the relative URL
			string normalizedRelativeUrl = NormalizeRelativeUrl(relativeUrl);

			// Use Uri constructor for safer URL combination
			var finalUri = new Uri(resourceUrl, normalizedRelativeUrl);

			return page.GotoAsync(finalUri.ToString(), options ?? new PageGotoOptions());
		}
		catch (UriFormatException ex)
		{
			throw new InvalidOperationException($"Failed to construct valid URL for resource '{aspireResourceName}' with relative URL '{relativeUrl}': {ex.Message}", ex);
		}
		catch (Exception ex) when (ex is not InvalidOperationException)
		{
			throw new InvalidOperationException($"Failed to navigate to Aspire resource '{aspireResourceName}': {ex.Message}", ex);
		}
	}

	/// <summary>
	/// Normalizes a relative URL by ensuring it starts with a forward slash and handles edge cases.
	/// </summary>
	/// <param name="relativeUrl">The relative URL to normalize.</param>
	/// <returns>A normalized relative URL.</returns>
	private static string NormalizeRelativeUrl(string? relativeUrl)
	{
		if (string.IsNullOrWhiteSpace(relativeUrl))
		{
			return "/";
		}

		// Trim whitespace
		relativeUrl = relativeUrl.Trim();

		// Ensure it starts with a forward slash
		if (!relativeUrl.StartsWith('/'))
		{
			relativeUrl = "/" + relativeUrl;
		}

		// Basic validation - reject URLs that might be attempting to navigate to different hosts
		if (relativeUrl.Contains("://") || relativeUrl.StartsWith("//"))
		{
			throw new ArgumentException("Relative URL cannot contain protocol or authority components.", nameof(relativeUrl));
		}

		return relativeUrl;
	}

}
