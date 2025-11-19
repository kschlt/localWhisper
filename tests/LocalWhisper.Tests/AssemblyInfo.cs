using Xunit;

// Suppress verbose xUnit diagnostic messages
[assembly: CollectionBehavior(DisableTestParallelization = false, MaxParallelThreads = -1)]

// Configure xUnit to suppress diagnostic messages
[assembly: TestFramework("Xunit.Sdk.TestFramework", "xunit.execution.desktop")]
