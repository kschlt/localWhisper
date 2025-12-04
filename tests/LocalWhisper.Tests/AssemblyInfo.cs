using Xunit;

// Disable test parallelization to prevent WPF STA thread deadlocks and resource contention
// Running WPF tests in parallel can cause indefinite hangs due to Dispatcher/threading issues
[assembly: CollectionBehavior(DisableTestParallelization = true, MaxParallelThreads = 1)]

// Configure xUnit to suppress diagnostic messages
[assembly: TestFramework("Xunit.Sdk.TestFramework", "xunit.execution.desktop")]
