# ADR-0004: Autostart via Shortcut (REMOVED)

**Status:** Removed (Out of scope for v0.1)
**Date:** 2025-09-17
**Original Affected Requirement:** ~~FR-018~~ (removed)

---

## Context

The original plan included an "Autostart" feature to launch the app automatically when Windows starts. This would be implemented via a shortcut (`.lnk`) in the user's Startup folder (`%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\`).

**Original Requirements:**
- Checkbox in wizard: "Start with Windows"
- Creates shortcut to EXE in Startup folder
- No admin rights required (user-level startup, not system-level)
- Option in settings to enable/disable autostart
- Shortcut removed during reset/uninstall

---

## Decision

**We remove the autostart feature from v0.1.**

**Rationale:**
1. **Scope reduction:** Focus on core dictation workflow; autostart is a "nice to have."
2. **Time constraint:** Solo developer, 40-60 hour budget; autostart adds complexity.
3. **Repair complexity:** If EXE is moved, shortcut becomes invalid and requires repair.
4. **User workaround:** Power users can manually create shortcut if needed.
5. **Future enhancement:** Can add in v0.2+ with better shortcut management (update on EXE move, etc.).

---

## Consequences

### Positive

✅ **Simpler wizard:** One less step (wizard is now 3 steps instead of 4).

✅ **Less code:** No shortcut creation/deletion/validation logic needed.

✅ **Faster delivery:** Saves ~4-6 hours of development and testing.

✅ **No repair edge cases:** No need to handle invalid shortcuts if EXE moves.

### Negative

❌ **User must manually start app:** Users who want autostart must create shortcut themselves.
  - **Mitigation:**
    - README includes instructions for manual autostart:
      1. Right-click EXE → "Create Shortcut"
      2. Move shortcut to `%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\`
    - Consider adding autostart in v0.2.

---

## User Workaround (Manual Autostart)

**Instructions for README:**

> **To start the app automatically with Windows:**
> 1. Right-click `SpeechClipboard.exe` and select "Create Shortcut."
> 2. Open the Startup folder:
>    - Press `Win+R`, type `shell:startup`, press Enter.
> 3. Move the shortcut into the Startup folder.
> 4. App will now start when you log in to Windows.

---

## Future Consideration (v0.2+)

**If we re-add autostart:**
- Include in settings (not wizard initially).
- Automatically update shortcut target if EXE path changes.
- Use `IWshRuntimeLibrary` (COM) or `ShellLink` P/Invoke to create `.lnk` files programmatically.
- Test shortcut validity on app startup; offer to repair if broken.

---

## Impact on Other Documentation

**Removed:**
- ~~FR-018: Autostart (without Admin)~~
- Wizard Step 3 checkbox: ~~"Start with Windows"~~
- UC-002 (First-Run Installation): Removed autostart step
- UC-004 (Reset/Uninstall): Removed shortcut deletion step
- NFR-002: Removed autostart mention

**Updated:**
- ADR-0000-index: Marked ADR-0004 as "Removed"
- Traceability matrix: Removed FR-018 entries

---

## Related Requirements

**Removed:**
- ~~FR-018: Autostart (without Admin)~~

**Affected:**
- UC-002: First-Run Installation (simplified wizard)
- UC-004: Reset/Uninstall (no longer removes autostart shortcut)

---

**Last updated:** 2025-09-17
**Version:** v1 (Removed status)
