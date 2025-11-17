# Project Structure

**Purpose:** Define the physical file/folder organization of the LocalWhisper codebase
**Audience:** Developers, AI agents
**Status:** Normative (implementation must follow this structure)
**Last Updated:** 2025-11-17

---

## Project Name

**Working Name:** `LocalWhisper`

**Rationale:** Descriptive name indicating local/offline Whisper-based speech-to-text. This is a working name and can be changed before v1.0 release.

**Usage in Code:**
- Solution name: `LocalWhisper.sln`
- Main project namespace: `LocalWhisper`
- Assembly name: `LocalWhisper.exe`
- Data root default: `%LOCALAPPDATA%\LocalWhisper\`

---

## Repository Layout

```
LocalWhisper/
├── src/
│   └── LocalWhisper/                    # Main WPF application
│       ├── LocalWhisper.csproj          # Project file (.NET 8)
│       ├── App.xaml                     # WPF application definition
│       ├── App.xaml.cs                  # Application entry point
│       │
│       ├── Core/                        # Domain logic & state
│       │   ├── StateMachine.cs          # App state management (Idle/Recording/Processing)
│       │   ├── ConfigManager.cs         # Config file read/write
│       │   ├── AppLogger.cs             # Logging wrapper (Serilog)
│       │   └── SlugGenerator.cs         # Filename slug generation (Iter 4)
│       │
│       ├── Services/                    # Application services
│       │   ├── HotkeyManager.cs         # Global hotkey registration (Iter 1)
│       │   ├── AudioRecorder.cs         # WASAPI audio recording (Iter 2)
│       │   ├── ClipboardService.cs      # Clipboard operations (Iter 4)
│       │   ├── HistoryWriter.cs         # History file creation (Iter 4)
│       │   ├── ModelManager.cs          # Model hash verification (Iter 5)
│       │   └── ResetService.cs          # Data root deletion (Iter 8)
│       │
│       ├── Adapters/                    # External tool integration
│       │   ├── WhisperCLIAdapter.cs     # Whisper subprocess management (Iter 3)
│       │   └── PostProcessorCLIAdapter.cs # Optional LLM integration (Iter 7)
│       │
│       ├── UI/                          # WPF UI components
│       │   ├── TrayIcon/
│       │   │   ├── TrayIconManager.cs   # System tray integration
│       │   │   └── TrayContextMenu.xaml # Right-click menu
│       │   ├── Dialogs/
│       │   │   ├── ErrorDialog.xaml     # User-friendly error messages
│       │   │   └── ConfirmDialog.xaml   # Confirmation dialogs
│       │   ├── Wizard/
│       │   │   ├── WizardWindow.xaml    # First-run setup (Iter 5)
│       │   │   ├── Step1_DataRoot.xaml
│       │   │   ├── Step2_Model.xaml
│       │   │   └── Step3_Hotkey.xaml
│       │   ├── Settings/
│       │   │   └── SettingsWindow.xaml  # Settings UI (Iter 6)
│       │   └── Flyout/
│       │       └── FlyoutNotification.xaml # Custom notification (Iter 4)
│       │
│       ├── Utils/                       # Utility classes
│       │   ├── Win32Interop.cs          # P/Invoke wrappers
│       │   ├── IconResources.cs         # Icon constants & helpers
│       │   └── PathHelpers.cs           # Path resolution & validation
│       │
│       └── Models/                      # Data models
│           ├── AppConfig.cs             # Config TOML schema
│           ├── TranscriptResult.cs      # STT output model
│           └── AppState.cs              # State enum & related types
│
├── tests/
│   └── LocalWhisper.Tests/              # Test project (xUnit)
│       ├── LocalWhisper.Tests.csproj
│       │
│       ├── Unit/                        # Unit tests (fast, isolated)
│       │   ├── StateMachineTests.cs
│       │   ├── SlugGeneratorTests.cs
│       │   └── ConfigManagerTests.cs
│       │
│       ├── Integration/                 # Integration tests (with I/O, processes)
│       │   ├── HotkeyManagerTests.cs
│       │   ├── AudioRecorderTests.cs
│       │   └── WhisperCLIAdapterTests.cs
│       │
│       ├── Features/                    # BDD scenarios (optional SpecFlow)
│       │   ├── Iteration1.feature       # Gherkin scenarios for Iter 1
│       │   ├── Iteration2.feature
│       │   └── ...
│       │
│       └── Fixtures/                    # Test data & helpers
│           ├── SampleAudio/             # WAV files for testing
│           ├── MockCLI/                 # Mock Whisper CLI for tests
│           └── TestHelpers.cs
│
├── docs/                                # Documentation (already exists)
│   ├── architecture/
│   ├── adr/
│   ├── iterations/
│   ├── specification/
│   ├── testing/
│   ├── ui/                              # UI style guides (new)
│   └── meta/
│
├── .gitignore                           # Standard .NET gitignore
├── LocalWhisper.sln                     # Visual Studio solution file
├── README.md                            # User-facing documentation
└── CLAUDE.md                            # AI agent instructions
```

---

## Project Evolution (Iteration Roadmap)

**Iteration 1 (Hotkey & Skeleton):**
- `Core/StateMachine.cs`
- `Core/AppLogger.cs`
- `Core/ConfigManager.cs` (minimal: hotkey only)
- `Services/HotkeyManager.cs`
- `UI/TrayIcon/TrayIconManager.cs`
- `UI/Dialogs/ErrorDialog.xaml`
- `Utils/Win32Interop.cs`
- `Utils/IconResources.cs`

**Iteration 2 (Audio):**
- `Services/AudioRecorder.cs`

**Iteration 3 (STT):**
- `Adapters/WhisperCLIAdapter.cs`

**Iteration 4 (Clipboard/History/Flyout):**
- `Services/ClipboardService.cs`
- `Services/HistoryWriter.cs`
- `Core/SlugGenerator.cs`
- `UI/Flyout/FlyoutNotification.xaml`

**Iteration 5 (Wizard):**
- `UI/Wizard/WizardWindow.xaml` + steps
- `Services/ModelManager.cs`
- `Core/ConfigManager.cs` (full schema)

**Iteration 6 (Settings):**
- `UI/Settings/SettingsWindow.xaml`

**Iteration 7 (Post-Processing):**
- `Adapters/PostProcessorCLIAdapter.cs`

**Iteration 8 (Stabilization/Reset):**
- `Services/ResetService.cs`
- Error handling hardening across all components

---

## Build & Packaging Strategy

### Development Build (Debug)

**Command:**
```bash
dotnet build src/LocalWhisper/LocalWhisper.csproj -c Debug
```

**Output:** `src/LocalWhisper/bin/Debug/net8.0-windows/LocalWhisper.exe` (framework-dependent)

---

### Release Build (Self-Contained, Portable)

**Command:**
```bash
dotnet publish src/LocalWhisper/LocalWhisper.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:PublishTrimmed=false
```

**Output:** `src/LocalWhisper/bin/Release/net8.0-windows/win-x64/publish/LocalWhisper.exe` (~150-200 MB)

**Notes:**
- `PublishTrimmed=false` to avoid WPF trimming issues (per ADR-0001)
- Single-file EXE for portability (NFR-002)

---

### Test Execution

**Run all tests:**
```bash
dotnet test tests/LocalWhisper.Tests/LocalWhisper.Tests.csproj
```

**Run specific test class:**
```bash
dotnet test --filter "FullyQualifiedName~StateMachineTests"
```

**Run tests with coverage:**
```bash
dotnet test --collect:"XPlat Code Coverage"
```

---

## Data Root (Runtime)

**Default location:** `%LOCALAPPDATA%\LocalWhisper\`

**Created by:** Application on first run (Iteration 1 minimal setup)

**Enhanced by:** Wizard (Iteration 5 allows user customization)

**Structure:** See `docs/specification/data-structures.md`

---

## Naming Conventions

### Namespaces
- Root: `LocalWhisper`
- Core logic: `LocalWhisper.Core`
- Services: `LocalWhisper.Services`
- Adapters: `LocalWhisper.Adapters`
- UI: `LocalWhisper.UI.<Component>`
- Utils: `LocalWhisper.Utils`

### File Naming
- Classes: `PascalCase.cs` (e.g., `StateMachine.cs`)
- XAML: `PascalCase.xaml` (e.g., `TrayContextMenu.xaml`)
- Interfaces: `IPascalCase.cs` (e.g., `IHotkeyManager.cs`)
- Enums: `PascalCase.cs` (e.g., `AppState.cs`)

### Test Naming
- Test classes: `<ComponentName>Tests.cs` (e.g., `StateMachineTests.cs`)
- BDD test classes: `US<ID>_<Name>Tests.cs` (e.g., `US001_HotkeyStateManagementTests.cs`)
- Test methods: `MethodName_Scenario_ExpectedResult` (e.g., `TransitionTo_IdleToRecording_UpdatesState`)

---

## Git Branching Strategy (for AI agents)

**Main branch:** `main` (or default branch name)

**Feature branches:** `claude/<session-id>` (auto-created by Claude Code)

**Commit message format:**
```
<type>(iter-<N>): <US-###> Brief description

- Detailed change 1
- Detailed change 2

DoD: [x] Code [x] Tests [x] Logs [x] Docs
```

**Types:** `feat`, `fix`, `refactor`, `docs`, `test`, `chore`

**Example:**
```
feat(iter-1): US-001 Implement hotkey state machine

- Add StateMachine class with Idle/Recording/Processing states
- Validate state transitions, throw InvalidStateTransitionException
- Add structured logging for all state changes
- Tests: StateMachineTests.cs (12 scenarios)

DoD: [x] Code [x] Tests [x] Logs [x] Docs
```

---

## IDE Recommendations

**Preferred:**
- Visual Studio 2022 (v17.8+) with WPF designer
- JetBrains Rider 2023.3+

**Alternative:**
- VS Code with C# Dev Kit extension (limited WPF designer support)

**Reason:** WPF XAML designer works best in Visual Studio/Rider.

---

## Dependencies (NuGet Packages)

**Iteration 1:**
```xml
<PackageReference Include="H.NotifyIcon.Wpf" Version="2.0.125" />
<PackageReference Include="Serilog" Version="3.1.1" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
<PackageReference Include="Tomlyn" Version="0.17.0" />
```

**Test Project:**
```xml
<PackageReference Include="xUnit" Version="2.6.2" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.5.4" />
<PackageReference Include="FluentAssertions" Version="6.12.0" />
<PackageReference Include="Moq" Version="4.20.70" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
```

**Optional (if using SpecFlow):**
```xml
<PackageReference Include="SpecFlow.xUnit" Version="3.9.74" />
```

---

## Related Documents

- **Architecture Overview:** `docs/architecture/architecture-overview.md`
- **Data Structures:** `docs/specification/data-structures.md`
- **Icon Style Guide:** `docs/ui/icon-style-guide.md` (new)
- **Testing Strategy:** `docs/testing/test-strategy.md`
- **ADR-0001:** Platform choice (.NET 8 + WPF)

---

**Last updated:** 2025-11-17
**Version:** v0.1 (Initial project structure for LocalWhisper)
