# Playwright Network Configuration

This document explains how to configure network access from the Playwright container to your Aspire services running on the host machine.

## Problem
By default, Docker containers cannot access services running on the host machine using `localhost`. This creates an issue when Playwright tests need to access your web applications during testing.

## Solution: host.docker.internal (Recommended)

The default configuration uses Docker's standard `host.docker.internal` hostname to access the host machine:

```csharp
var playwright = builder.AddPlaywright("playwright")
    .WithDataVolume();
```

This automatically maps `localhost` URLs in your Aspire service discovery to `host.docker.internal`, allowing your tests to work seamlessly.

### How it works

1. Your Aspire services run on the host machine with URLs like `http://localhost:7010`
2. The Playwright container automatically converts these to `http://host.docker.internal:7010`
3. Your tests work without any manual URL manipulation

## Alternative Configurations

### Custom host alias
You can specify a different hostname if needed:

```csharp
var playwright = builder.AddPlaywright("playwright")
    .WithDataVolume()
    .WithHostNetworkAccess("myhost"); // Use 'myhost' instead of 'host.docker.internal'
```

### Host networking mode (Linux/macOS only)
For maximum compatibility:

```csharp
var playwright = builder.AddPlaywright("playwright")
    .WithDataVolume()
    .WithContainerRuntimeArgs("--network=host");
```

## Test Code Example

With the default configuration, your test code is simple:

```csharp
public async Task CounterPage_ClickButton_IncrementsCounter()
{
    var webAppUrl = _configuration["services:webfrontend:https:0"]
        ?? _configuration["services:webfrontend:http:0"];
    
    // Automatic hostname conversion happens behind the scenes
    // Just handle HTTPS to HTTP conversion if needed for dev certificates
    if (webAppUrl?.StartsWith("https://") == true)
    {
        var isInContainer = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"));
        if (isInContainer)
        {
            webAppUrl = webAppUrl.Replace("https://", "http://");
        }
    }
    
    // Navigate directly to your application
    await page.GotoAsync($"{webAppUrl}/counter");
    // Rest of your test logic...
}
```

## Considerations

- **Platform**: `host.docker.internal` works on Windows and macOS. On Linux, it may require Docker Desktop or manual configuration
- **Certificates**: When accessing HTTPS endpoints from containers, you may need to convert to HTTP to avoid certificate validation issues
- **Performance**: Minimal performance overhead compared to other approaches
