# Aspire.Hosting.Playwright

A .NET Aspire hosting integration for Microsoft Playwright, allowing you to easily add a Playwright Docker container to your distributed application.

## Overview

This integration provides an easy way to run Playwright in a Docker container as part of your .NET Aspire application. It's based on the official Microsoft Playwright Docker image and includes support for remote browser automation, making it ideal for end-to-end testing and web scraping scenarios.

## Features

- 🐳 **Docker-based**: Uses the official `mcr.microsoft.com/playwright/dotnet` Docker image
- 🔗 **WebSocket Connection**: Exposes Playwright server on port 3000 with WebSocket endpoint
- 💾 **Persistent Storage**: Optional volume mounting for browser cache and data
- 🔧 **Development Mode**: Support for development environments with additional capabilities
- 🌐 **Host Network Access**: Optional access to host machine services
- ⚡ **Container Management**: Proper Docker container lifecycle management
- 🛡️ **Security**: Runs with non-root user by default (pwuser)

## Quick Start

### 1. Add the Package Reference

Add a reference to the Playwright hosting project in your AppHost:

```xml
<ProjectReference Include="../Aspire.Hosting.Playwright/Aspire.Hosting.Playwright.csproj" IsAspireProjectResource="false" />
```

### 2. Add Playwright to Your AppHost

```csharp
using Aspire.Hosting.Playwright;

var builder = DistributedApplication.CreateBuilder(args);

// Basic Playwright setup
var playwright = builder.AddPlaywright("playwright");

// Advanced setup with data volume and host network access
var playwrightAdvanced = builder.AddPlaywright("playwright-advanced")
    .WithDataVolume()  // Persistent browser cache
    .WithHostNetworkAccess();  // Access to host services

builder.Build().Run();
```

### 3. Connect from Your Application

```csharp
using Microsoft.Playwright;

// Get the connection string from configuration
var playwrightUrl = builder.Configuration.GetConnectionString("playwright");

// Connect to the remote Playwright server
using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.ConnectAsync(playwrightUrl);

var page = await browser.NewPageAsync();
await page.GotoAsync("https://example.com");
```

## Configuration Options

### Basic Configuration

```csharp
// Simple setup with default configuration
var playwright = builder.AddPlaywright("playwright");

// Custom port
var playwright = builder.AddPlaywright("playwright", port: 3001);
```

### Data Persistence

```csharp
// Add a data volume for browser cache and downloads
var playwright = builder.AddPlaywright("playwright")
    .WithDataVolume();

// Custom volume name
var playwright = builder.AddPlaywright("playwright")
    .WithDataVolume("my-playwright-data");
```

### Development Mode

```csharp
// Enable development mode (adds SYS_ADMIN capability)
// Only use in development environments
var playwright = builder.AddPlaywright("playwright")
    .WithDevelopmentMode();
```

### Host Network Access

```csharp
// Allow Playwright container to access host services
// Services running on host can be accessed via 'hostmachine' hostname
var playwright = builder.AddPlaywright("playwright")
    .WithHostNetworkAccess();
```

## Docker Configuration

The integration automatically configures the Playwright container with:

- **Image**: `mcr.microsoft.com/playwright/dotnet:v1.53.0-noble`
- **Port**: 3000 (WebSocket server)
- **User**: `pwuser` (non-root for security)
- **Working Directory**: `/home/pwuser`
- **Runtime Args**: `--init --ipc=host` for proper process handling and shared memory
- **Environment Variables**:
  - `PLAYWRIGHT_BROWSERS_PATH=/ms-playwright`
  - `PLAYWRIGHT_SKIP_BROWSER_DOWNLOAD=1`

## Container Health

The Playwright container doesn't expose a standard HTTP health check endpoint since it runs a WebSocket server. The container is considered ready when it starts successfully and begins listening on port 3000. You can verify connectivity by attempting to connect via the WebSocket endpoint.

## Security Considerations

- **Default User**: Runs as `pwuser` (non-root) for better security
- **Development Mode**: Only use `WithDevelopmentMode()` in development environments as it adds elevated privileges
- **Host Access**: Be cautious with `WithHostNetworkAccess()` in production environments

## Browser Versions

The current integration uses Playwright v1.53.0 with the following browsers:

- Chromium
- Firefox  
- WebKit (Safari)

All browsers are pre-installed in the Docker image.

## Examples

### End-to-End Testing Setup

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.MyApi>("api");
var webApp = builder.AddProject<Projects.MyWebApp>("webapp")
    .WithReference(apiService);

// Playwright for E2E testing with access to local services
var playwright = builder.AddPlaywright("playwright")
    .WithHostNetworkAccess()  // Access webapp and api via 'hostmachine'
    .WithDataVolume();        // Persist browser cache

builder.Build().Run();
```

### Web Scraping Setup

```csharp
var playwright = builder.AddPlaywright("playwright")
    .WithDataVolume("scraper-cache")  // Persistent cache for scraping sessions
    .WithDevelopmentMode();           // For development only

// In your scraping service
var page = await browser.NewPageAsync();
await page.GotoAsync("https://example.com");
var content = await page.InnerTextAsync("body");
```

## Troubleshooting

### Common Issues

1. **Connection Refused**: Ensure the container is fully started before connecting
2. **Browser Crashes**: Try using `WithDevelopmentMode()` for development environments
3. **Memory Issues**: The `--ipc=host` flag is automatically added to prevent shared memory issues
4. **Host Access**: Use `hostmachine` instead of `localhost` when accessing host services

### Debugging

Check the container logs in the Aspire dashboard for detailed error information.

## Contributing

Feel free to contribute to this integration by submitting issues or pull requests.

## License

This project follows the same license as .NET Aspire.
