var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.Sample_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

var webFrontend = builder.AddProject<Projects.Sample_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

// Basic Playwright container setup with localhost access
var playwright = builder.AddPlaywright("playwright")
    .WithDataVolume();  // Add persistent volume for browser cache

// You could also set up Playwright with development mode (adds SYS_ADMIN capability)
// var playwrightDev = builder.AddPlaywright("playwright-dev")
//     .WithDevelopmentMode()  // Adds --cap-add=SYS_ADMIN for development
//     .WithDataVolume();

// Add the test project with references to both Playwright and the web frontend
var tests = builder.AddProject<Projects.Sample_Tests>("tests")
    .WithReference(playwright)
    .WithReference(webFrontend)
    .WaitFor(playwright)
    .WaitFor(webFrontend);

builder.Build().Run();
