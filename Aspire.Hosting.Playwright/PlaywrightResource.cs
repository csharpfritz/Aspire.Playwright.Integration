namespace Aspire.Hosting;

/// <summary>
/// A resource that represents a Playwright container.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class PlaywrightResource(string name) : ContainerResource(name), IResourceWithConnectionString
{
	/// <summary>
	/// The default port for the Playwright server.
	/// </summary>
	public const int DEFAULT_PORT = 3000;

	/// <summary>
	/// The default image repository for Playwright.
	/// </summary>
	public const string DEFAULT_IMAGE_REPOSITORY = "mcr.microsoft.com";

	/// <summary>
	/// The default image name for Playwright.
	/// </summary>
	public const string DEFAULT_IMAGE_NAME = "playwright";

	/// <summary>
	/// The default image tag for Playwright.
	/// </summary>
	public const string DEFAULT_IMAGE_TAG = "v1.53.0-noble";

	/// <summary>
	/// The Playwright version that corresponds to the Docker image.
	/// </summary>
	public const string PLAYWRIGHT_VERSION = "1.53.0";

	/// <summary>
	/// The endpoint name for the Playwright server.
	/// </summary>
	internal const string PlaywrightEndpointName = "ws";
	private EndpointReference? _primaryEndpointReference;

	public EndpointReference PrimaryEndpoint => _primaryEndpointReference ??= new(this, PlaywrightEndpointName);

	/// <summary>
	/// Gets the connection string expression for the Playwright endpoint.
	/// </summary>
	public ReferenceExpression ConnectionStringExpression =>
		ReferenceExpression.Create($"ws://{PrimaryEndpoint.Property(EndpointProperty.HostAndPort)}");
}