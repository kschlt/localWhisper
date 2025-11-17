# ADR-0003: Storage Layout & Data Root

**Status:** Accepted
**Date:** 2025-09-17
**Affected Requirements:** FR-014, FR-017, FR-019, FR-024; NFR-002, NFR-003, NFR-006

---

## Context

We need to define where and how the application stores its data: configuration, models, history transcripts, logs, and temporary files.

**Requirements:**
- Portable deployment (no admin rights, no `Program Files` writes)
- User must be able to backup/migrate all app data easily
- History files must be organized for easy browsing and search
- Configuration should be human-editable
- Logs should be accessible for troubleshooting
- Temporary files should be cleaned up automatically

**Constraints:**
- Windows environment (use Windows-friendly paths and conventions)
- User may have limited disk space (allow custom location)
- User may want to sync data (e.g., via Dropbox, OneDrive)

---

## Options Considered

### Option A: Single Data Root (User-Configurable, Default `%LOCALAPPDATA%`)

**Description:**
All app data lives under a single "Data Root" folder, with predefined subfolders.

**Structure:**
```
<DATA_ROOT>/
  config/       ← Configuration files
  models/       ← Whisper model files
  history/      ← Transcripts (date-organized)
  logs/         ← Application logs
  tmp/          ← Temporary files (WAV, JSON)
```

**Default location:** `%LOCALAPPDATA%\SpeechClipboardApp\`

**Pros:**
+ **Portability:** User can move/copy entire folder to new machine or USB drive.
+ **Easy backup:** Backup `<DATA_ROOT>` backs up everything.
+ **Clear ownership:** All app data in one place, no scattered files.
+ **User-configurable:** Can choose location during wizard (e.g., `D:\MyApps\`).
+ **Sync-friendly:** Can place in Dropbox/OneDrive folder for cloud sync.

**Cons:**
- Requires repair flow if folder is moved/deleted (UC-003).

---

### Option B: Separate Standard Windows Locations

**Description:**
Use Windows standard folders:
- Config: `%APPDATA%\SpeechClipboardApp\config.toml`
- Models: `%LOCALAPPDATA%\SpeechClipboardApp\models\`
- History: `%USERPROFILE%\Documents\SpeechClipboard\`
- Logs: `%LOCALAPPDATA%\SpeechClipboardApp\logs\`
- Temp: `%TEMP%\SpeechClipboardApp\`

**Pros:**
+ **Standard Windows conventions:** Each data type in "correct" place.

**Cons:**
- **Scattered data:** Harder to backup/migrate (multiple locations).
- **Confusing for users:** "Where is my history?" → multiple places.
- **Less portable:** Cannot easily move app to new machine.
- **Sync complexity:** Would need to sync multiple folders.

---

### Option C: App-Relative (Next to EXE)

**Description:**
Store all data in subfolders next to the app EXE.

**Structure:**
```
C:\Tools\SpeechClipboard\
  SpeechClipboard.exe
  config/
  models/
  history/
  logs/
  tmp/
```

**Pros:**
+ **Ultra-portable:** Move EXE + folders as one unit.
+ **Simple:** Everything in one place.

**Cons:**
- **Risky if EXE is in read-only location:** User might install in `Program Files` (unintentionally).
- **Clutters EXE directory:** Less clean.
- **Backup complexity:** User must remember to back up EXE folder, not just data.

---

## Decision

We choose **Option A: Single Data Root (User-Configurable)**.

**Rationale:**
1. **Portability:** Single folder is easiest to backup, migrate, and sync.
2. **User control:** Wizard allows user to choose location (default is sensible, custom is possible).
3. **Clear data ownership:** All app data in one place, easy to find/delete.
4. **Sync-friendly:** User can choose Dropbox/OneDrive folder if desired.
5. **Repair flow is acceptable:** UC-003 handles case where folder is moved/deleted.

**Default location:** `%LOCALAPPDATA%\SpeechClipboardApp\`
- Writable without admin rights
- Standard for portable app data on Windows
- Not synced by default (unlike `%APPDATA%`), which is better for large files (models)

---

## Consequences

### Positive

✅ **Easy backup/migration:** User can copy `<DATA_ROOT>` to USB/cloud and restore on new machine.

✅ **Clear uninstall:** Reset function deletes entire `<DATA_ROOT>` folder (FR-019).

✅ **User-friendly:** Users understand "all my data is in this folder."

✅ **Sync-friendly:** Power users can choose cloud-synced location (e.g., `%USERPROFILE%\Dropbox\SpeechClipboardApp\`).

✅ **Portable app pattern:** Aligns with other portable apps (7-Zip, Notepad++, etc.).

### Negative

❌ **Repair flow needed:** If user moves folder without updating config, app must detect and offer repair (UC-003).
  - **Mitigation:**
    - App checks if `<DATA_ROOT>` is accessible on startup.
    - If not, show dialog: "Data folder not found. Choose new location or reset."
    - Log repair actions for diagnostics.

❌ **Large models in user folder:** Whisper model (~1 GB) may surprise users with disk usage.
  - **Mitigation:**
    - Wizard shows estimated disk usage (~1.5 GB for small model + overhead).
    - User can choose different drive if needed.

### Neutral

⚪ **Network drive risk:** User might choose network drive; if offline, app fails to start.
  - **Mitigation:** Wizard warns "Recommended: local drive for best performance."

---

## Detailed Structure

### Data Root Layout

```
<DATA_ROOT>/
  ├── config/
  │   ├── config.toml           # Main configuration
  │   └── glossary.txt          # Optional: user-defined glossary
  │
  ├── models/
  │   └── ggml-small-de.gguf    # Whisper model (or other variants)
  │
  ├── history/
  │   └── YYYY/                 # Year (e.g., 2025)
  │       └── YYYY-MM/          # Year-Month (e.g., 2025-09)
  │           └── YYYY-MM-DD/   # Year-Month-Day (e.g., 2025-09-17)
  │               ├── YYYYMMDD_HHMMSS_<slug>.md
  │               └── YYYYMMDD_HHMMSS_<slug>.md
  │
  ├── logs/
  │   ├── app.log               # Current log
  │   ├── app.log.1             # Rotated log
  │   └── ...
  │
  └── tmp/
      ├── rec_20250917_143022.wav   # Temp audio files
      └── stt_result.json            # Temp STT output
```

### History Folder Structure (Date-Based)

**Rationale:**
- **Easy browsing:** Users can navigate by date in File Explorer.
- **Scalable:** Thousands of transcripts won't clutter a single folder.
- **Searchable:** Windows search indexes files by date; easy to find "all transcripts from September 2025."
- **Archival-friendly:** User can zip/delete old year folders to save space.

**Depth:** 3 levels (`YYYY/YYYY-MM/YYYY-MM-DD/`) balances organization vs. deep nesting.

---

## Configuration Management

### Config File Path

**Absolute path stored:** App stores full path to `<DATA_ROOT>` in `config.toml` (which is inside `<DATA_ROOT>/config/`).

**Bootstrap problem:** On first run, how does app know where `<DATA_ROOT>` is?
- **Solution:** App tries default location (`%LOCALAPPDATA%\SpeechClipboardApp\`).
- If config not found → Launch wizard.
- Wizard creates `<DATA_ROOT>` and `config/config.toml`.

**Path relocation:** If user moves `<DATA_ROOT>`, app detects mismatch on startup:
- Config says path is `C:\OldPath\`, but config file is at `D:\NewPath\config\config.toml`.
- App shows repair dialog (UC-003).

---

## Temporary File Cleanup

**Strategy:**
- WAV and JSON files in `tmp/` are deleted after successful processing.
- On app startup, delete any `tmp/` files older than 24 hours (leftover from crashes).
- Log cleanup actions.

**Implementation:**
```csharp
void CleanupTempFolder()
{
    var tmpPath = Path.Combine(dataRoot, "tmp");
    var cutoff = DateTime.Now.AddDays(-1);

    foreach (var file in Directory.GetFiles(tmpPath))
    {
        if (File.GetCreationTime(file) < cutoff)
        {
            try
            {
                File.Delete(file);
                logger.LogInformation("Deleted old temp file: {File}", file);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete temp file: {File}", file);
            }
        }
    }
}
```

---

## Migration & Versioning

**Future Consideration:**
- If `<DATA_ROOT>` structure changes in v0.2, include migration logic:
  - Detect old structure (e.g., missing `tmp/` folder).
  - Create new folders, move files as needed.
  - Log migration event.

**Backward compatibility:**
- v0.1 structure should remain stable.
- Additive changes (new subfolders) are safe; existing files remain readable.

---

## Related Decisions

- **ADR-0001:** .NET platform enables easy path manipulation and File I/O.
- **ADR-0002:** `tmp/` folder stores WAV and JSON for CLI subprocess communication.

---

## Related Requirements

**Functional:**
- FR-014: History file → Uses `history/YYYY/MM/DD/` structure
- FR-017: Model management → Stores models in `models/`, validates with SHA-256
- FR-019: Reset/Uninstall → Deletes entire `<DATA_ROOT>`
- FR-024: Slug generation → Filename pattern includes slug

**Non-Functional:**
- NFR-002: Portability → User-writable location, no admin rights
- NFR-003: Reliability → Repair flow if data root is missing
- NFR-006: Observability → Logs in `logs/` with rotation

---

## Related Documents

- **Data Structures:** `specification/data-structures.md` (full file format details)
- **Use Cases:** `specification/use-cases.md` (UC-002, UC-003, UC-004)

---

**Last updated:** 2025-09-17
**Version:** v1
