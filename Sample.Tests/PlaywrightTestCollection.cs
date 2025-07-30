using Xunit;

namespace Sample.Tests;

/// <summary>
/// Test collection definition to ensure PlaywrightFixture is shared across all tests.
/// This ensures only one browser connection is created for all tests in the collection.
/// </summary>
[CollectionDefinition("Playwright")]
public class PlaywrightTestCollection : ICollectionFixture<PlaywrightFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
