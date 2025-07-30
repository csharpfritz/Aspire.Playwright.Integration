using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Playwright;

/// <summary>
/// This class manages connecting to the Playwright service resource within Aspire.
/// </summary>
internal class PlaywrightService
{

    private PlaywrightService() { }

    public static PlaywrightService Instance { get; } = new();

    internal Uri? PlaywrightServiceResource { get; private set; }
    internal ILogger<PlaywrightService>? Logger { get; private set; }
    internal PlaywrightConnectionOptions? Options { get; private set; }

    /// <summary>
    /// Configures the Playwright service with the provided options and logger.
    /// </summary>
    /// <param name="options">The connection options.</param>
    /// <param name="logger">The logger instance.</param>
    internal void Configure(PlaywrightConnectionOptions options, ILogger<PlaywrightService> logger)
    {
        Options = options;
        Logger = logger;
        PlaywrightServiceResource = options.ServiceUri;
    }

	private static readonly Dictionary<string, Uri> ResourceUrlCache = new();

	internal Uri ResolveResourceUrl(string aspireResourceName)
	{
		if (string.IsNullOrWhiteSpace(aspireResourceName))
		{
			throw new ArgumentException("Resource name cannot be null or empty.", nameof(aspireResourceName));
		}

		Logger?.LogDebug("Starting URL resolution for resource: {ResourceName}", aspireResourceName);

		if (ResourceUrlCache.TryGetValue(aspireResourceName, out var cachedUrl))
		{
			Logger?.LogDebug("Found cached URL for '{ResourceName}': {CachedUrl}", aspireResourceName, cachedUrl);
			return cachedUrl;
		}

		var webAppUrl = string.Empty; // Configuration[$"services:{aspireResourceName}:https:0"];
		Logger?.LogDebug("Initial HTTPS lookup for '{ResourceName}': {HttpsUrl}", aspireResourceName, webAppUrl ?? "null");
		
		webAppUrl = !string.IsNullOrEmpty(webAppUrl) ? webAppUrl.TrimEnd('/') : Configuration[$"services:{aspireResourceName}:http:0"];
		Logger?.LogDebug("After HTTPS/HTTP resolution for '{ResourceName}': {ResolvedUrl}", aspireResourceName, webAppUrl ?? "null");

		if (string.IsNullOrEmpty(webAppUrl))
		{
			Logger?.LogError("No connection string found for resource '{ResourceName}'", aspireResourceName);
			throw new InvalidOperationException($"Resource connection string for '{aspireResourceName}' not found. Make sure the service is referenced.");
		}

		if (webAppUrl.Contains("localhost"))
		{
			Logger?.LogDebug("Original URL contains localhost: {OriginalUrl}", webAppUrl);
			webAppUrl = webAppUrl.Replace("localhost", "host.docker.internal");
			Logger?.LogDebug("Replaced localhost with host.docker.internal: {UpdatedUrl}", webAppUrl);

		if (webAppUrl.StartsWith("https://"))
			{
				Logger?.LogDebug("URL starts with HTTPS, checking container context");

				// Check if we're running in a container context
				var isInContainer = true; // !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER")) ||
									// !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT"));

				// Logger?.LogDebug("Container context check - DOTNET_RUNNING_IN_CONTAINER: {ContainerVar}, ASPIRE_ALLOW_UNSECURED_TRANSPORT: {AspireVar}, IsInContainer: {IsInContainer}", 
				// 	Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") ?? "null",
				// 	Environment.GetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT") ?? "null",
				// 	isInContainer);

				if (isInContainer)
				{
					Logger?.LogDebug("Converting HTTPS to HTTP for container communication. Before: {HttpsUrl}", webAppUrl);
					// Convert HTTPS to HTTP for container-to-host communication
					webAppUrl = webAppUrl.Replace("https://", "http://");
					Logger?.LogDebug("After HTTPS to HTTP conversion: {HttpUrl}", webAppUrl);
				}
			}

		}

		
		Logger?.LogInformation("Final resolved resource URL for '{ResourceName}': {ResourceUrl}", aspireResourceName, webAppUrl);

		var resourceUrl = new Uri(webAppUrl);
		ResourceUrlCache[aspireResourceName] = resourceUrl;
		Logger?.LogDebug("Cached URL for '{ResourceName}': {CachedUrl}", aspireResourceName, resourceUrl);
		return resourceUrl;	}

	/// <summary>
	/// Checks if the service has been properly configured.
	/// </summary>
	internal bool IsConfigured => PlaywrightServiceResource != null && Logger != null;

	public IConfiguration Configuration { get; internal set; }
}
