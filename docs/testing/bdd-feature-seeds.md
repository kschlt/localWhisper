# BDD Feature Seeds

**Purpose:** Gherkin scenarios for behavior-driven development (acceptance tests)
**Tool:** SpecFlow or similar BDD framework
**Status:** Seeds provided; full step definitions to be implemented during iterations
**Last Updated:** 2025-09-17

---

## Overview

These feature files define executable specifications using Gherkin syntax. Each scenario maps to acceptance criteria from user stories.

**Format:**
```gherkin
Feature: Feature Name
  Description...

  @TagName @AnotherTag
  Scenario: Scenario Name
    Given [precondition]
    When [action]
    Then [expected outcome]
```

**Tags:**
- `@Iter-{N}`: Iteration number
- `@UC-{ID}`: Use case ID
- `@FR-{ID}`: Functional requirement ID
- `@NFR-{ID}`: Non-functional requirement ID

---

## Feature: Dictate to Clipboard (Core Flow)

**File:** `features/DictateToClipboard.feature`

```gherkin
@UC-001 @Iter-4 @FR-010 @FR-012 @FR-013 @FR-014 @FR-015 @FR-024
Feature: Dictate text to clipboard
  As a knowledge worker
  I want to dictate speech and have it appear in my clipboard
  So that I can quickly paste it into any application

  Background:
    Given die App läuft
    And ein Whisper-Modell ist konfiguriert
    And der Hotkey ist "Ctrl+Shift+D"

  @Iter-4 @NFR-001
  Scenario: Erfolgreiches Diktat via Hold-to-Talk
    When ich den Hotkey gedrückt halte
    And ich für 5 Sekunden spreche
    And ich den Hotkey loslasse
    Then steht der transkribierte Text im System-Clipboard within 3 seconds
    And eine History-Datei wurde erstellt unter "history/YYYY/YYYY-MM/YYYY-MM-DD/"
    And die History-Datei enthält Front-Matter mit "created", "lang", "stt_model", "duration_sec"
    And ein Flyout ist sichtbar mit der Nachricht "Transkript im Clipboard"
    And der Flyout verschwindet nach 3 Sekunden

  @Iter-4 @FR-024
  Scenario: Slug-Generierung für History-Datei
    Given ich sage "Let me check on that and get back to you"
    When die Transkription abgeschlossen ist
    Then ist der Dateiname "YYYYMMDD_HHMMSS_let-me-check-on-that-and-get.md"

  @Iter-1 @FR-010
  Scenario: Hotkey wechselt State zu Recording
    When ich den Hotkey drücke
    Then ist der App-State "Recording"
    And das Tray-Icon zeigt Recording-Status

  @Iter-1 @FR-010
  Scenario: Hotkey wechselt State zurück zu Idle
    Given der App-State ist "Recording"
    When ich den Hotkey loslasse
    Then ist der App-State "Processing"
    And nach Abschluss der Verarbeitung ist der App-State "Idle"

  @Iter-4 @NFR-004
  Scenario: Flyout erscheint schnell nach Clipboard-Schreiben
    Given die Transkription ist abgeschlossen
    And der Text wurde ins Clipboard geschrieben
    Then erscheint der Flyout innerhalb von 500 Millisekunden
```

---

## Feature: Audio Recording

**File:** `features/AudioRecording.feature`

```gherkin
@Iter-2 @FR-011
Feature: Audio Recording
  As the app
  I need to record audio from the microphone
  So that it can be transcribed

  @Iter-2
  Scenario: WAV-Datei wird erstellt mit korrektem Format
    Given der Hotkey ist gedrückt
    When ich für 5 Sekunden spreche
    And ich den Hotkey loslasse
    Then existiert eine WAV-Datei im tmp/ Ordner
    And die WAV-Datei hat 16 kHz Sample-Rate
    And die WAV-Datei ist Mono
    And die WAV-Datei ist 16-bit PCM
    And die WAV-Datei ist mindestens 1 Sekunde lang

  @Iter-2 @FR-011
  Scenario: Aufnahme-Dauer entspricht Hotkey-Haltezeit
    When ich den Hotkey für 3 Sekunden halte
    Then ist die WAV-Datei ungefähr 3 Sekunden lang (±0.5s)

  @Iter-2 @FR-021
  Scenario: Fehler bei fehlendem Mikrofon
    Given das Mikrofon ist nicht verfügbar
    When ich den Hotkey drücke
    Then erscheint ein Fehlerdialog "Mikrofon nicht verfügbar"
    And die App stürzt nicht ab
    And der App-State ist "Idle"
    And der Fehler wird geloggt
```

---

## Feature: STT with Whisper

**File:** `features/STT.feature`

```gherkin
@Iter-3 @FR-012
Feature: Speech-to-Text with Whisper
  As the app
  I need to transcribe audio using Whisper
  So that speech becomes text

  Background:
    Given eine WAV-Datei liegt vor im tmp/ Ordner
    And das Whisper-Modell ist konfiguriert

  @Iter-3
  Scenario: STT erzeugt JSON gemäß Kontrakt v1
    When whisper-cli.exe aufgerufen wird
    Then existiert stt_result.json
    And die JSON-Datei enthält "text" (nicht leer)
    And die JSON-Datei enthält "language"
    And die JSON-Datei enthält "duration_sec"

  @Iter-3 @FR-012
  Scenario: Whisper CLI Exit-Code 0 bedeutet Erfolg
    When whisper-cli.exe erfolgreich läuft
    Then ist der Exit-Code 0
    And der Adapter liefert das Transkript zurück

  @Iter-3 @FR-021
  Scenario: Whisper CLI Exit-Code 2 bedeutet Modell-Fehler
    Given das Modell ist nicht vorhanden
    When whisper-cli.exe aufgerufen wird
    Then ist der Exit-Code 2
    And erscheint ein Fehlerdialog "Modell nicht gefunden"
    And die App stürzt nicht ab

  @Iter-3 @FR-021 @NFR-003
  Scenario: Timeout bei STT verhindert Hängen
    Given whisper-cli.exe hängt für 120 Sekunden
    When 60 Sekunden vergangen sind
    Then wird der Prozess abgebrochen
    And erscheint ein Fehlerdialog "Transkription dauerte zu lange"
    And die App bleibt responsiv
```

---

## Feature: First-Run Wizard

**File:** `features/FirstRunWizard.feature`

```gherkin
@UC-002 @Iter-5 @FR-016 @FR-017
Feature: First-Run Wizard
  As a new user
  I want to set up the app easily
  So that I can start using it without technical knowledge

  Background:
    Given die App wird zum ersten Mal gestartet
    And keine config.toml existiert

  @Iter-5 @NFR-004
  Scenario: Wizard-Abschluss in unter 2 Minuten
    Given der Wizard öffnet sich
    When ich den Standard-Datenordner akzeptiere
    And ich "Modell herunterladen" wähle
    And der Download abgeschlossen ist
    And ich den Standard-Hotkey akzeptiere
    And ich auf "Fertig" klicke
    Then ist die Gesamtzeit < 2 Minuten
    And die App startet normal

  @Iter-5 @FR-017
  Scenario: Modell-Prüfung mit korrektem Hash
    Given ich im Wizard-Schritt 2 bin
    When das Modell heruntergeladen wurde
    Then wird der SHA-256-Hash berechnet
    And der Hash stimmt mit dem erwarteten Wert überein
    And "Modell OK ✓" wird angezeigt
    And "Weiter" ist aktiviert

  @Iter-5 @FR-017
  Scenario: Modell-Prüfung mit falschem Hash
    Given ich im Wizard-Schritt 2 bin
    And ich wähle eine Datei mit falschem Hash
    Then wird "Modell ungültig oder beschädigt" angezeigt
    And "Weiter" ist deaktiviert
    And ich kann "Erneut versuchen" klicken

  @Iter-5 @FR-021
  Scenario: Schreibschutz-Fehler bei Ordner-Auswahl
    Given ich im Wizard-Schritt 1 bin
    When ich einen schreibgeschützten Ordner wähle
    Then erscheint "Ordner ist schreibgeschützt"
    And ich kann einen anderen Ordner wählen
    And der Wizard bleibt offen
```

---

## Feature: Repair and Reset

**File:** `features/RepairAndReset.feature`

```gherkin
@UC-003 @UC-004 @Iter-5 @Iter-8 @FR-016 @FR-019 @FR-021
Feature: Repair and Reset
  As a user
  I want the app to recover from configuration issues
  And I want to cleanly uninstall if needed

  @Iter-5 @UC-003
  Scenario: Repair-Flow wenn Daten-Root verschoben
    Given die App war vorher konfiguriert
    And der Daten-Root wurde verschoben
    When ich die App starte
    Then erscheint "Datenordner nicht gefunden"
    And ich kann "Neuen Ordner wählen" auswählen
    And ich wähle den verschobenen Ordner
    Then startet die App normal
    And die History-Dateien sind zugänglich

  @Iter-8 @UC-004 @FR-019
  Scenario: Reset löscht Daten-Root
    Given die App ist installiert und konfiguriert
    When ich "Zurücksetzen/Deinstallieren..." wähle
    And ich klicke "Alles löschen"
    And ich bestätige
    Then wird der Daten-Root gelöscht
    And erscheint "Daten gelöscht. Bitte löschen Sie die EXE manuell."
    And die App beendet sich
    And keine App-Ordner existieren mehr unter dem Daten-Root

  @Iter-8 @UC-004
  Scenario: "Nur Einstellungen löschen" behält History
    Given die App ist installiert
    When ich "Zurücksetzen/Deinstallieren..." wähle
    And ich klicke "Nur Einstellungen löschen"
    Then werden config/ und logs/ gelöscht
    And history/ bleibt erhalten
    And die App beendet sich
```

---

## Feature: Error Cases

**File:** `features/ErrorCases.feature`

```gherkin
@FR-021 @NFR-003
Feature: Error Cases
  As the app
  I must handle errors gracefully
  So that the user experience is not disrupted

  @Iter-1 @FR-021
  Scenario: Hotkey-Konflikt wird erkannt
    Given ein anderer Prozess hat "Ctrl+Shift+D" registriert
    When die App versucht den Hotkey zu registrieren
    Then erscheint "Hotkey bereits belegt"
    And die App startet trotzdem
    And die App bleibt stabil

  @Iter-2 @FR-021
  Scenario: Mikrofon gesperrt
    Given das Mikrofon wird von einer anderen App verwendet
    When ich den Hotkey drücke
    Then erscheint "Mikrofon nicht verfügbar"
    And der App-State kehrt zu "Idle" zurück
    And die App stürzt nicht ab

  @Iter-4 @FR-021
  Scenario: Disk voll beim History-Schreiben
    Given die Festplatte ist voll
    When eine Transkription abgeschlossen ist
    Then wird der Text ins Clipboard geschrieben
    And die History-Schreibung schlägt fehl
    And erscheint ein Flyout "History konnte nicht gespeichert werden"
    And der Fehler wird geloggt

  @Iter-8 @NFR-003
  Scenario: Alle Fehler sind geloggt und App bleibt stabil
    Given ein beliebiger Fehler tritt auf
    Then erscheint ein benutzerfreundlicher Dialog
    And der Fehler wird mit Kontext geloggt (Zeitstempel, Komponente, Aktion)
    And die App stürzt nicht ab
    And die App kehrt zu einem stabilen State zurück
```

---

## Feature: Settings

**File:** `features/Settings.feature`

```gherkin
@Iter-6 @FR-020
Feature: Settings
  As a user
  I want to change app configuration
  So that I can customize it to my preferences

  @Iter-6
  Scenario: Hotkey ändern
    Given die Einstellungen sind geöffnet
    When ich einen neuen Hotkey "Ctrl+Alt+D" setze
    And ich auf "Speichern" klicke
    Then ist der Hotkey in config.toml aktualisiert
    And die App registriert den neuen Hotkey
    And der alte Hotkey ist deaktiviert

  @Iter-6 @FR-020
  Scenario: Dateiformat von .md auf .txt ändern
    Given die Einstellungen sind geöffnet
    When ich "Dateiformat" auf "txt" ändere
    And ich auf "Speichern" klicke
    Then werden neue History-Dateien als .txt gespeichert
    And bestehende .md Dateien bleiben unverändert
```

---

## Feature: Post-Processing

**File:** `features/PostProcessing.feature`

```gherkin
@Iter-7 @FR-022
Feature: Optional Post-Processing
  As a user
  I want to optionally improve transcription formatting
  So that my text is more readable

  Background:
    Given Post-Processing ist aktiviert
    And ein LLM CLI ist konfiguriert

  @Iter-7
  Scenario: Post-Processing formatiert Text
    Given das Whisper-Transkript ist "lets meet at 3pm asap"
    When Post-Processing ausgeführt wird
    Then ist das Endergebnis "Let's meet at 3pm, as soon as possible."
    And der Text wird ins Clipboard geschrieben
    And die History-Datei enthält den formatierten Text

  @Iter-7 @FR-022
  Scenario: Fallback bei Post-Processing-Fehler
    Given das Whisper-Transkript ist "Original text"
    And der LLM CLI schlägt fehl (Exit-Code 1)
    When Post-Processing ausgeführt wird
    Then wird das Original-Transkript verwendet
    And erscheint ein Flyout "Post-Processing fehlgeschlagen (Original-Text verwendet)"
    And der Text wird ins Clipboard geschrieben
    And der Fehler wird geloggt
```

---

## Performance Tests (Manual Measurement)

**These are not Gherkin scenarios but manual test procedures:**

### NFR-001: p95 Latency ≤ 2.5s

**Procedure:**
1. Prepare 100 WAV files (mix of 3s, 5s, 10s)
2. For each:
   - Trigger dictation
   - Measure time: hotkey release → clipboard write
3. Calculate p50, p95, p99
4. Verify: p95 ≤ 2.5s

### NFR-004: Flyout Latency ≤ 0.5s

**Procedure:**
1. Complete a dictation
2. Measure time: clipboard write → flyout visible
3. Verify: ≤ 0.5s

---

## Related Documents

- **Test Strategy:** `testing/test-strategy.md`
- **Functional Requirements:** `specification/functional-requirements.md`
- **Use Cases:** `specification/use-cases.md`
- **Iteration Plan:** `iterations/iteration-plan.md`

---

**Last updated:** 2025-09-17
**Version:** v0.1 (Initial BDD seeds)
