# Testing Quick Start

## Run Tests Locally

```bash
# All tests (fast, unit + integration only)
dotnet test --filter "Category!=WpfIntegration"

# Specific test class
dotnet test --filter "FullyQualifiedName~ModelValidatorTests"

# With verbose output
dotnet test --filter "Category!=WpfIntegration" --verbosity detailed
```

## Test Categories

| Category | Count | Speed | Run In CI? |
|----------|-------|-------|------------|
| **Unit Tests** | 60+ | Fast (< 1s) | ✅ Yes |
| **Integration Tests** | 6 | Medium (< 5s) | ✅ Yes |
| **WPF UI Tests** | 56 | Slow + Crashes | ❌ No (skipped) |

## Why Are UI Tests Skipped?

**Short answer:** They're integration tests testing WPF windows directly, which is fragile and slow.

**Long answer:** See [tests/LocalWhisper.Tests/README.md](LocalWhisper.Tests/README.md)

**Coverage:** Manual testing via `docs/testing/manual-test-script-iter6.md`

**Future:** Refactor to MVVM pattern, test ViewModels instead (v1.0)

## Adding New Tests

### ✅ DO: Unit Test for Business Logic

```csharp
[Fact]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange
    var service = new MyService();

    // Act
    var result = service.DoSomething();

    // Assert
    result.Should().Be(expected);
}
```

### ❌ DON'T: Integration Test for UI

```csharp
// ❌ BAD: Testing WPF window directly
[StaFact]
public void WindowTest()
{
    var window = new MyWindow();
    window.TextBox.Text.Should().Be("something");
}
```

Instead: Extract ViewModel and test that!

## Test Structure

```
tests/LocalWhisper.Tests/
├── Unit/                    ← Unit tests (fast)
│   ├── Core/               ← Core business logic
│   ├── Services/           ← Service layer
│   └── [WPF tests marked with [Trait("Category", "WpfIntegration")]]
├── Integration/             ← Integration tests
└── README.md               ← Full documentation
```

## Current Test Status

- **Passing:** 60+ unit + integration tests ✅
- **Skipped:** 56 WPF UI tests ⚠️
- **Coverage:** ~80% core business logic ✅
- **CI:** Green build ✅

## Resources

- **Full Strategy:** [tests/LocalWhisper.Tests/README.md](LocalWhisper.Tests/README.md)
- **Manual Tests:** `docs/testing/manual-test-script-iter*.md`
- **FluentAssertions:** https://fluentassertions.com/
