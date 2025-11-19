# Batch Testing Guide

All test files have been organized into 6 batches to manage test output size.

## Test Batch Organization

### Batch 1 - Core State Machine & Audio (5 files)
- StateMachineTests.cs
- StateMachinePostProcessingTests.cs
- AudioRecorderTests.cs
- WhisperCLIAdapterTests.cs
- WavValidatorTests.cs

### Batch 2 - Configuration (4 files)
- ConfigManagerTests.cs
- ConfigManagerPostProcessingTests.cs
- PostProcessingConfigTests.cs
- DataRootValidatorTests.cs

### Batch 3 - Settings & UI Changes (6 files)
- SettingsWindowTests.cs
- SettingsPersistenceTests.cs
- HotkeyChangeTests.cs
- DataRootChangeTests.cs
- FileFormatChangeTests.cs
- LanguageChangeTests.cs

### Batch 4 - Model & Post-Processing (5 files)
- ModelValidatorTests.cs
- ModelVerificationTests.cs
- ModelDownloaderTests.cs
- LlmPostProcessorTests.cs
- GlossaryLoaderTests.cs

### Batch 5 - Wizard & Utilities (5 files)
- WizardManagerTests.cs
- TrayMenuTests.cs
- RestartLogicTests.cs
- HistoryWriterTests.cs
- SlugGeneratorTests.cs

### Batch 6 - Integration (1 file)
- PostProcessingIntegrationTests.cs

## How to Run Tests by Batch

### Run a specific batch:
```bash
dotnet test LocalWhisper.sln --filter "Batch=1" --verbosity normal
```

### Run all tests:
```bash
dotnet test LocalWhisper.sln --verbosity normal
```

## Workflow

1. Run Batch 1 tests
2. Share ONLY the failures from Batch 1 with Claude
3. Claude fixes Batch 1
4. Move to Batch 2, repeat

This approach keeps context manageable and allows systematic fixing of all tests.
