# Use Cases

**Purpose:** High-level user-facing scenarios for the Dictate-to-Clipboard application
**Status:** Stable (v0.1 baseline)
**Last Updated:** 2025-09-17

---

## UC-001 — Quick Dictation (Hold-to-Talk)

**Primary Actor:** Knowledge worker (laptop user)

**Goal:** Capture spoken input quickly and paste it into any application without UI friction.

**Preconditions:**
- App is running (visible only as tray icon)
- Hotkey is configured
- Whisper model is available
- Microphone is accessible

**Main Success Scenario:**

1. User holds down the configured hotkey (e.g., `Ctrl+Shift+D`)
2. App enters Recording state (tray icon changes to indicate recording)
3. User speaks their message/idea/note
4. User releases the hotkey
5. App stops recording and begins transcription
6. App transcribes audio using Whisper
7. App writes transcript to system clipboard
8. App saves transcript to history file (with timestamp and slug-based filename)
9. App displays custom flyout: "Transkript im Clipboard"
10. User presses `Ctrl+V` in target application (chat, document, email, etc.)
11. Transcript appears in target application

**Postconditions:**
- Transcript is in system clipboard (ready to paste)
- History file exists: `history/YYYY/YYYY-MM/YYYY-MM-DD/YYYYMMDD_HHMMSS_<slug>.md`
- App returns to Idle state
- User can immediately start another dictation

**Extensions (Error Cases):**

**3a. Microphone not accessible:**
- System shows error dialog: "Mikrofon nicht verfügbar. Bitte überprüfen Sie die Berechtigungen."
- App returns to Idle state
- Error is logged

**3b. User releases hotkey before speaking (silent recording):**
- STT runs but produces empty or very short text
- Empty transcript is NOT written to clipboard or history (or written with note "[empty]")
- App returns to Idle state

**6a. STT fails (model missing, timeout, crash):**
- System shows error dialog: "Transkription fehlgeschlagen. Bitte prüfen Sie die Modellkonfiguration."
- Error is logged with details
- App returns to Idle state (does not crash)

**8a. History directory not writable (permissions, disk full):**
- Clipboard write still succeeds (prioritize user's immediate need)
- Error dialog: "History konnte nicht gespeichert werden. Prüfen Sie den Datenordner."
- Error is logged

**Frequency:** Very high (multiple times per hour for active users)

**Related Requirements:**
- FR-010 (Hotkey)
- FR-011 (Audio recording)
- FR-012 (STT)
- FR-013 (Clipboard)
- FR-014 (History)
- FR-015 (Flyout)
- FR-024 (Slug generation)
- NFR-001 (Latency ≤ 2.5s p95)
- NFR-004 (Flyout ≤ 0.5s)

**Related Iterations:** 1, 2, 3, 4, 8

---

## UC-002 — First-Time Installation (No Admin Rights)

**Primary Actor:** New user installing the app on a Windows laptop

**Goal:** Set up the app for first use without requiring administrator privileges.

**Preconditions:**
- User has downloaded the portable EXE
- User has placed EXE in desired location (e.g., `C:\Tools\` or `D:\MyApps\`)
- No admin rights required

**Main Success Scenario:**

1. User double-clicks the EXE
2. App detects this is the first run (no config file exists)
3. App opens the First-Run Wizard (3 steps)

**Step 1: Data Root Selection**
4. Wizard shows default location: `%LOCALAPPDATA%\SpeechClipboardApp\`
5. User accepts default OR chooses custom folder (e.g., on D: drive for cloud sync)
6. App verifies folder is writable
7. App creates folder structure: `config/`, `models/`, `history/`, `logs/`, `tmp/`

**Step 2: Model Setup**
8. Wizard prompts for Whisper model:
   - Option A: "Download model 'small' for German/English" (default, recommended)
   - Option B: "I have already downloaded the model" (browse for file)
9. User selects Option A → App downloads model to `<DATA_ROOT>/models/`
10. App computes SHA-256 hash and compares to known-good hash
11. Wizard shows "Modell OK ✓"

**Step 3: Hotkey & Autostart**
12. Wizard prompts for hotkey selection (default: `Ctrl+Shift+D`)
13. User accepts default OR chooses custom combination
14. App verifies hotkey is not already registered (warns if conflict)
15. ~~Wizard shows checkbox: "Start with Windows" (autostart)~~ (REMOVED - out of scope for v0.1)
16. ~~User checks/unchecks as desired~~ (REMOVED)

**Finalization**
17. Wizard saves configuration to `config/config.toml`
18. Wizard closes
19. App starts normally (tray icon appears)
20. User can now perform UC-001 (Quick Dictation)

**Postconditions:**
- Config file exists and contains valid settings
- Whisper model is verified and ready
- Hotkey is registered
- App is running and ready to use
- ~~Autostart shortcut created (if selected)~~ (REMOVED)

**Extensions (Error Cases):**

**6a. Chosen folder is not writable:**
- Wizard shows error: "Ordner ist schreibgeschützt. Bitte wählen Sie einen anderen Ordner."
- User returns to Step 1

**10a. Model download fails (network error):**
- Wizard shows error: "Download fehlgeschlagen. Bitte prüfen Sie Ihre Internetverbindung."
- User can retry OR choose Option B (provide own model)

**10b. Model hash mismatch:**
- Wizard shows error: "Modell ist ungültig oder beschädigt. Bitte laden Sie es erneut herunter."
- User can retry download OR browse for different file

**14a. Hotkey conflict:**
- Wizard shows warning: "Hotkey bereits belegt. Bitte wählen Sie eine andere Kombination."
- User returns to hotkey selection

**Frequency:** Once per installation (or per major version upgrade)

**Related Requirements:**
- FR-016 (Wizard)
- FR-017 (Model management & hash check)
- ~~FR-018 (Autostart)~~ (REMOVED - out of scope)
- FR-020 (Settings persistence)
- NFR-002 (Portable, no admin)
- NFR-004 (Usability: wizard < 2 min)
- NFR-006 (Observability: log all choices)

**Related Iterations:** 5, 6

---

## UC-003 — Data Root Missing or Moved (Repair Flow)

**Primary Actor:** Existing user whose data folder was deleted, moved, or is on unavailable network drive

**Goal:** Restore app functionality by locating or re-creating the data root.

**Preconditions:**
- App was previously configured (config file existed)
- Data root is now missing or inaccessible

**Main Success Scenario:**

1. User starts the app (e.g., after reboot, or after moving folders)
2. App tries to load config from expected location
3. App detects config file is missing OR data root path is invalid
4. App shows repair dialog: "Datenordner nicht gefunden. Möchten Sie einen neuen Ordner wählen oder neu einrichten?"
   - Option A: "Neuen Ordner wählen" (re-link to moved folder)
   - Option B: "Neu einrichten" (start wizard from scratch)
5. User selects Option A
6. App opens folder picker
7. User navigates to the moved folder (e.g., `D:\Backup\SpeechClipboardApp\`)
8. App verifies folder structure (checks for `config/`, `models/`, `history/`)
9. App updates config path and continues startup
10. App starts normally

**Postconditions:**
- Config points to correct data root
- App is functional
- Existing history is preserved (if folder was just moved)

**Extensions (Error Cases):**

**8a. Selected folder does not contain valid structure:**
- Dialog: "Ordner scheint kein gültiger Datenordner zu sein. Möchten Sie ihn neu initialisieren oder einen anderen wählen?"
- User chooses: "Initialisieren" → App creates missing subfolders; OR "Anderen wählen" → back to Step 6

**8b. Folder is not writable:**
- Dialog: "Ordner ist schreibgeschützt. Bitte wählen Sie einen anderen Ordner."
- User returns to Step 6

**5b. User selects Option B (start fresh):**
- App launches First-Run Wizard (UC-002)
- Old data is orphaned (not deleted, user can manually recover)

**Frequency:** Rare (only after manual folder moves, drive letters changing, etc.)

**Related Requirements:**
- FR-016 (Wizard repair path)
- FR-021 (Error dialogs)
- FR-024 (Path validation)
- NFR-003 (Reliability: no crashes)
- NFR-006 (Observability: log repair actions)

**Related Iterations:** 5, 8

---

## UC-004 — Reset and Uninstall

**Primary Actor:** User who wants to completely remove the app and its data

**Goal:** Clean uninstallation with option to remove all app data.

**Preconditions:**
- App is installed and configured

**Main Success Scenario:**

1. User right-clicks tray icon
2. User selects "Zurücksetzen/Deinstallieren..."
3. App shows confirmation dialog:
   - "Dies wird alle Einstellungen und gespeicherten Transkripte entfernen. Fortfahren?"
   - Buttons: "Abbrechen" | "Nur Einstellungen löschen" | "Alles löschen"
4. User clicks "Alles löschen"
5. App ~~removes autostart shortcut from `%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\`~~ (REMOVED - autostart out of scope)
6. App deletes entire data root folder (recursively): `config/`, `models/`, `history/`, `logs/`, `tmp/`
7. App shows final message: "Daten gelöscht. Bitte löschen Sie die EXE-Datei manuell."
8. App exits

**Postconditions:**
- ~~Autostart shortcut is removed~~ (REMOVED)
- Data root folder is deleted
- EXE file remains (user must delete manually)
- No registry entries (there were none to begin with)

**Alternative Flows:**

**4a. User clicks "Nur Einstellungen löschen":**
- App deletes `config/` and `logs/`
- App preserves `history/` (user's transcripts)
- App shows: "Einstellungen gelöscht. Beim nächsten Start wird der Wizard angezeigt."
- App exits

**4b. User clicks "Abbrechen":**
- Dialog closes, no changes made

**Extensions (Error Cases):**

**6a. Data root deletion fails (file in use, permissions):**
- App shows error: "Einige Dateien konnten nicht gelöscht werden. Bitte schließen Sie andere Programme und versuchen Sie es erneut."
- App logs which files failed to delete
- App exits

**Frequency:** Rare (only during actual uninstallation or troubleshooting)

**Related Requirements:**
- FR-019 (Reset/Uninstall)
- ~~FR-018 (Remove autostart)~~ (REMOVED - out of scope)
- NFR-003 (Reliability: clean removal)

**Related Iterations:** 8

---

## Use Case Prioritization

| UC | Priority | Frequency | Risk | Iteration(s) |
|----|----------|-----------|------|--------------|
| UC-001 | Critical | Very High | Medium (STT integration, performance) | 1-4, 8 |
| UC-002 | Critical | Low (once per install) | Medium (UX complexity) | 5, 6 |
| UC-003 | High | Rare | Low | 5, 8 |
| UC-004 | Medium | Very Rare | Low | 8 |

---

## Related Documents

- **Functional Requirements:** `specification/functional-requirements.md`
- **Non-Functional Requirements:** `specification/non-functional-requirements.md`
- **Traceability Matrix:** `specification/traceability-matrix.md`
- **Iteration Plan:** `iterations/iteration-plan.md`

---

**Last updated:** 2025-09-17
**Version:** v0.1 (Initial use cases; autostart removed from UC-002 and UC-004)
