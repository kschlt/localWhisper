# ADR-0001: Platform — .NET 8 + WPF

**Status:** Accepted
**Date:** 2025-09-17
**Affected Requirements:** FR-010, FR-015, FR-016..020, FR-021, FR-023; NFR-001, NFR-002, NFR-004, NFR-006

---

## Context

We need to choose a platform for building a Windows desktop application with the following characteristics:

**Requirements:**
- Global hotkey registration (system-wide, works when app is not in focus)
- System tray integration (minimize to tray, context menu)
- Native Windows UI (wizard, settings dialogs, custom notifications)
- Portable deployment (no admin rights required, single EXE)
- Audio recording via WASAPI
- Clipboard access
- File system operations
- Process management (launch external CLI tools)
- Reasonable development velocity (solo developer, 40-60 hour project)

**Constraints:**
- Must run on Windows 10/11 (no cross-platform requirement for v0.1)
- Must support self-contained deployment
- Must be stable and well-documented
- Should leverage existing ecosystem and libraries

---

## Options Considered

### Option A: .NET 8 + WPF (Windows Presentation Foundation)

**Description:** Use .NET 8 (latest LTS) with WPF for UI framework.

**Pros:**
+ Excellent Windows integration (hotkeys, tray, clipboard, WASAPI via NAudio)
+ Mature ecosystem (NuGet libraries for audio, TOML, logging)
+ Self-contained publish supported (`PublishSingleFile=true`)
+ Strong IDE support (Visual Studio, Rider)
+ Good documentation and community
+ High development velocity (C# is productive)
+ Built-in async/await for background tasks
+ Easy interop with Win32 APIs (P/Invoke)

**Cons:**
- Larger EXE size (~150-200 MB self-contained)
- Cold start can be slower (~1-2 seconds first run)
- Windows-only (but that's acceptable for v0.1)
- Requires SmartScreen bypass without code signing

---

### Option B: Rust + wry/tao (or native Windows API)

**Description:** Use Rust with either a lightweight web-based UI (wry) or pure Win32.

**Pros:**
+ Smaller binary size (~10-20 MB)
+ Fast startup
+ Memory safety guarantees
+ High performance

**Cons:**
- Steeper learning curve (especially Win32 interop)
- Less mature UI ecosystem (wry is relatively new)
- Longer development time (estimated +50% vs. .NET)
- Fewer libraries for Windows-specific tasks (WASAPI, TOML, etc.)
- Async ecosystem is less mature than .NET's

---

### Option C: Electron + Node.js

**Description:** Use Electron for cross-platform desktop app with web technologies.

**Pros:**
+ Cross-platform (Windows, Mac, Linux)
+ Rich UI capabilities (HTML/CSS/JS)
+ Large ecosystem (npm)

**Cons:**
- Much larger binary (~200-300 MB)
- Higher memory footprint (~150 MB+ idle)
- Worse Windows integration (hotkeys, tray are more complex)
- Slower performance for native operations (subprocess overhead)
- Not ideal for a performance-sensitive background app

---

### Option D: C++ + Qt or Win32

**Description:** Use C++ with either Qt framework or pure Win32 API.

**Pros:**
+ Excellent performance
+ Small binary (with static linking)
+ Full control over Windows APIs

**Cons:**
- Longest development time (manual memory management, verbose API)
- More error-prone (memory leaks, crashes)
- Smaller ecosystem for modern tooling (logging, config, etc.)
- Not ideal for solo developer with limited time

---

## Decision

We choose **.NET 8 + WPF** (Option A).

**Rationale:**
1. **Development velocity** is critical for a solo developer targeting 40-60 hours. .NET offers the best productivity.
2. **Windows integration** is first-class: hotkeys, tray, clipboard, audio are all well-supported via libraries or P/Invoke.
3. **Portability requirement** (self-contained, no admin) is fully met by .NET's publish model.
4. **Ecosystem maturity** reduces risk: NAudio for WASAPI, Tomlyn for TOML, Serilog for logging, etc.
5. **Binary size trade-off** is acceptable: portability and dev velocity outweigh the larger EXE size.

---

## Consequences

### Positive

✅ **Fast development:** Wizard, dialogs, tray app can be implemented quickly with WPF.

✅ **Reliable Windows integration:** Proven libraries and patterns for hotkeys, audio, clipboard.

✅ **Testability:** Easy to write unit and integration tests with .NET tooling.

✅ **Future extensibility:** If we need web service, database, or other features later, .NET has excellent support.

✅ **Strong typing and safety:** C# prevents many classes of bugs (vs. JavaScript or manual C++ memory management).

### Negative

❌ **Larger binary:** ~150-200 MB self-contained EXE (vs. 10-20 MB with Rust).
  - **Mitigation:** Users accept this size for desktop apps; portability is more valuable.

❌ **Slower cold start:** First launch may take 1-2 seconds as runtime extracts.
  - **Mitigation:** Subsequent starts are faster; this is one-time cost.

❌ **SmartScreen warnings:** Without code signing, users see "Unknown publisher" warning.
  - **Mitigation:** README will guide users; code signing planned for v0.2+.

❌ **Windows-only (initially):** .NET 8 is cross-platform, but WPF is Windows-only.
  - **Mitigation:** Mac/Linux support is out of scope for v0.1; could use Avalonia in future if needed.

### Neutral

⚪ **Performance:** Adequate for our use case (NFR-001 targets are achievable).

---

## Implementation Notes

**Key Libraries:**
- **NAudio** or **CSCore**: WASAPI audio recording
- **Tomlyn**: TOML configuration parsing
- **Serilog** or **NLog**: Structured logging
- **Hardcodet.NotifyIcon.Wpf**: Enhanced system tray support for WPF

**Publish Settings:**
```xml
<PublishSingleFile>true</PublishSingleFile>
<SelfContained>true</SelfContained>
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
<PublishTrimmed>false</PublishTrimmed> <!-- Keep false to avoid WPF trimming issues -->
```

**Testing:**
- Unit tests: xUnit or NUnit
- BDD tests: SpecFlow (Gherkin support for acceptance tests)

---

## Related Decisions

- **ADR-0002:** CLI subprocesses (complementary decision on external tool integration)
- **ADR-0005:** Custom flyout (WPF enables custom UI implementation)

---

## Related Requirements

**Functional:**
- FR-010: Hotkey registration → Win32 `RegisterHotKey` via P/Invoke
- FR-011: Audio recording → NAudio library
- FR-015: Custom flyout → WPF Window with custom styling
- FR-016..020: Wizard, settings, dialogs → WPF UI
- FR-021: Error dialogs → WPF `MessageBox` or custom dialogs
- FR-023: Logging → Serilog

**Non-Functional:**
- NFR-001: Performance → .NET async/await for background tasks
- NFR-002: Portability → Self-contained publish
- NFR-004: Usability → WPF provides responsive UI
- NFR-006: Observability → Serilog structured logging

---

**Last updated:** 2025-09-17
**Version:** v1
