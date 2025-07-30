using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding a Playwright container to the application model.
/// </summary>
public static class BuilderExtensions
{
	/// <summary>
	/// Adds the Playwright container to the application model.
	/// </summary>
	/// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
	/// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
	/// <param name="port">An optional fixed port to bind to the Playwright container. This will be provided randomly by Aspire if not set.</param>
	/// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
	public static IResourceBuilder<PlaywrightResource> AddPlaywright(
		this IDistributedApplicationBuilder builder,
		[ResourceName] string name,
		int? port = null)
	{
		ArgumentNullException.ThrowIfNull(builder, nameof(builder));
		ArgumentNullException.ThrowIfNull(name, nameof(name));

		var resource = new PlaywrightResource(name);

		port ??= 30003;

		return builder.AddResource(resource)
			.WithAnnotation(new ContainerImageAnnotation
			{
				Image = PlaywrightResource.DEFAULT_IMAGE_NAME,
				Tag = PlaywrightResource.DEFAULT_IMAGE_TAG,
				Registry = PlaywrightResource.DEFAULT_IMAGE_REPOSITORY
			})
			.WithHttpEndpoint(port: port, targetPort: PlaywrightResource.DEFAULT_PORT, name: PlaywrightResource.PlaywrightEndpointName)
			.WithHttpHealthCheck("/json/list", endpointName: PlaywrightResource.PlaywrightEndpointName)  // Check Playwright's WebSocket debug endpoint for readiness
			.PublishAsConnectionString()
			.WithArgs("npx", "-y", $"playwright@{PlaywrightResource.PLAYWRIGHT_VERSION}", "run-server", "--port", PlaywrightResource.DEFAULT_PORT.ToString(), "--host", "0.0.0.0")
			.WithContainerRuntimeArgs("--add-host", "host.docker.internal:host-gateway", "--init", "--ipc=host")
			.WithEnvironment("PLAYWRIGHT_BROWSERS_PATH", "/ms-playwright")
			.WithEnvironment("PLAYWRIGHT_SKIP_BROWSER_DOWNLOAD", "1");
	}

	/// <summary>
	/// Adds a volume to the Playwright container for storing browser data and cache.
	/// </summary>
	/// <param name="builder">The <see cref="IResourceBuilder{T}"/>.</param>
	/// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
	/// <param name="isReadOnly">A flag that indicates if this is a read-only volume.</param>
	/// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
	public static IResourceBuilder<PlaywrightResource> WithDataVolume(this IResourceBuilder<PlaywrightResource> builder, string? name = null, bool isReadOnly = false)
	{
		ArgumentNullException.ThrowIfNull(builder, nameof(builder));

		return builder.WithVolume(name ?? $"{builder.Resource.Name}-data", "/home/pwuser/.cache/playwright", isReadOnly);
	}

	/// <summary>
	/// Enables development mode for Playwright container by adding SYS_ADMIN capability.
	/// This can help resolve certain Chromium-related issues during development.
	/// </summary>
	/// <param name="builder">The <see cref="IResourceBuilder{T}"/>.</param>
	/// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
	/// <remarks>
	/// This adds the SYS_ADMIN capability which may be needed for certain Chromium features.
	/// Only use this in development environments.
	/// </remarks>
	public static IResourceBuilder<PlaywrightResource> WithDevelopmentMode(this IResourceBuilder<PlaywrightResource> builder)
	{
		ArgumentNullException.ThrowIfNull(builder, nameof(builder));

		return builder.WithContainerRuntimeArgs("--cap-add=SYS_ADMIN");
	}

	/// <summary>
	/// Adds network access to the host machine from within the Playwright container.
	/// </summary>
	/// <param name="builder">The <see cref="IResourceBuilder{T}"/>.</param>
	/// <param name="hostAlias">The hostname alias to use for the host machine. Defaults to 'host.docker.internal'.</param>
	/// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
	/// <remarks>
	/// This allows the Playwright container to access services running on the host machine
	/// using the specified hostname alias. 'host.docker.internal' is Docker's standard
	/// hostname for accessing the host machine from containers.
	/// </remarks>
	internal static IResourceBuilder<PlaywrightResource> WithHostNetworkAccess(this IResourceBuilder<PlaywrightResource> builder, string hostAlias = "host.docker.internal")
	{
		ArgumentNullException.ThrowIfNull(builder, nameof(builder));

		return builder.WithContainerRuntimeArgs($"--add-host={hostAlias}:host-gateway");
	}
}
