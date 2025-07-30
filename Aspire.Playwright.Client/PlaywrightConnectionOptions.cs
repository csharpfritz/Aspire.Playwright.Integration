namespace Aspire.Hosting.Playwright;

/// <summary>
/// Configuration options for Playwright connections.
/// </summary>
public class PlaywrightConnectionOptions
{
	/// <summary>
	/// Maximum number of connection attempts. Defaults to 5.
	/// </summary>
	public int MaxRetries { get; init; } = 5;

	/// <summary>
	/// Initial delay between retries. Defaults to 2 seconds.
	/// </summary>
	public TimeSpan InitialRetryDelay { get; init; } = TimeSpan.FromSeconds(2);

	/// <summary>
	/// Default connection options.
	/// </summary>
	public static PlaywrightConnectionOptions Default => new();

	public Uri? ServiceUri { get; internal set; }

	internal string PlaywrightServiceResourceName { get; set; } = "playwright";
}
