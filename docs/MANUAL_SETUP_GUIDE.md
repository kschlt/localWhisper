# LocalWhisper Manual Setup Guide

This guide documents the manual setup steps required to run LocalWhisper in a portable configuration without requiring administrator rights or installing Visual C++ Redistributables.

## Overview

LocalWhisper requires three main components:
1. **Whisper CLI** - For speech-to-text transcription
2. **Llama CLI** - For optional LLM post-processing
3. **Model Files** - Whisper model for STT and Llama model for post-processing

## Prerequisites

- Windows 10/11 (x64)
- ~4 GB free disk space (depending on model sizes)
- CUDA-capable GPU (optional, for faster LLM post-processing)

## Step 1: Download Whisper CLI

1. Go to the [whisper.cpp releases page](https://github.com/ggerganov/whisper.cpp/releases)
2. Download `whisper-bin-x64.zip` (latest release)
3. Extract the archive to a temporary folder
4. You will need these files:
   - `whisper-cli.exe`
   - `whisper.dll`
   - `SDL2.dll`
   - All `ggml*.dll` files (e.g., `ggml-base.dll`, `ggml-cpu.dll`, etc.)

## Step 2: Download Whisper Model

1. Go to [HuggingFace whisper.cpp models](https://huggingface.co/ggerganov/whisper.cpp/tree/main)
2. Download your preferred model (recommended: `ggml-small.bin` or `ggml-small.en.bin`)
   - **small.en** (~466 MB) - English-only, faster
   - **small** (~466 MB) - Multilingual (German, English, etc.)
   - **medium** (~1.5 GB) - Better accuracy, slower
   - **large-v3** (~3.1 GB) - Best accuracy, slowest

## Step 3: Download Llama CLI (Optional - for Post-Processing)

**Note:** Only required if you want to enable LLM post-processing to improve transcript formatting.

### Option A: CUDA (NVIDIA GPU)
1. Go to [llama.cpp releases page](https://github.com/ggerganov/llama.cpp/releases)
2. Download `llama-*-bin-win-cuda-cu*.zip` (e.g., `llama-b1234-bin-win-cuda-cu12.2.0-x64.zip`)
3. Extract the archive
4. Locate `llama-cli.exe` (NOT `llama-llava-cli.exe`)

### Option B: CPU-only
1. Download `llama-*-bin-win-avx2-x64.zip`
2. Extract and locate `llama-cli.exe`

## Step 4: Download Llama Model (Optional)

**Note:** Only required if you want to enable LLM post-processing.

1. Go to [HuggingFace](https://huggingface.co)
2. Download a quantized Llama model (recommended: `Llama-3.2-3B-Instruct-Q4_K_M.gguf`)
   - Search for "llama 3.2 3b instruct gguf"
   - File size: ~1.87 GB
   - Q4_K_M quantization provides good quality with reasonable size

## Step 5: Create Folder Structure

Create the following folders in `C:\Users\<YourUsername>\AppData\Local\LocalWhisper\`:

```
LocalWhisper/
├── bin/                  # Executables and DLLs
├── config/               # Configuration files (auto-created by wizard)
├── models/               # Model files
├── history/              # Dictation history (auto-created)
├── logs/                 # Application logs (auto-created)
└── tmp/                  # Temporary audio files (auto-created)
    └── failed/           # Failed recordings (auto-created)
```

**You only need to manually create:**
- `bin/`
- `models/`

The wizard will create the rest during first run.

## Step 6: Copy Files to Folder Structure

### Copy to `bin/` folder:

From whisper-bin-x64.zip:
- `whisper-cli.exe`
- `whisper.dll`
- `SDL2.dll`
- All `ggml*.dll` files

From llama.cpp (if using post-processing):
- `llama-cli.exe`
- Any required CUDA DLLs (automatically included in the zip)

**Example `bin/` folder contents:**
```
bin/
├── whisper-cli.exe
├── whisper.dll
├── SDL2.dll
├── ggml-base.dll
├── ggml-cpu.dll
├── ggml-cuda.dll           (if using CUDA)
├── llama-cli.exe           (if using post-processing)
└── [other ggml DLLs]
```

### Copy to `models/` folder:

- Your downloaded Whisper model (e.g., `ggml-small.bin`)
- Your downloaded Llama model (e.g., `Llama-3.2-3B-Instruct-Q4_K_M.gguf`) [if using post-processing]

**Example `models/` folder contents:**
```
models/
├── ggml-small.bin
└── Llama-3.2-3B-Instruct-Q4_K_M.gguf    (optional)
```

## Step 7: Run LocalWhisper for First Time

1. Launch `LocalWhisper.exe`
2. The **First-Run Wizard** will appear
3. Follow the wizard steps:
   - **Step 1: Data Root** - Accept default or choose custom location
   - **Step 2: Model Selection** - Select language and model, then browse to your downloaded `.bin` file
   - **Step 3: Hotkey** - Choose your recording hotkey (default: Ctrl+Shift+A)
   - **Step 4: Completion** - Click "Fertig" (Finish)

The wizard will:
- Create remaining folder structure
- Generate initial `config.toml`
- Copy your selected model file to the models/ folder

## Step 8: Configure Paths Manually (Required)

After completing the wizard, you need to manually edit `config.toml` to add the correct paths:

1. Navigate to: `C:\Users\<YourUsername>\AppData\Local\LocalWhisper\config\`
2. Open `config.toml` in a text editor (e.g., Notepad)
3. Update the following sections:

### Update Whisper CLI path:

```toml
[whisper]
cli_path = "C:\\Users\\<YourUsername>\\AppData\\Local\\LocalWhisper\\bin\\whisper-cli.exe"
model_path = "C:\\Users\\<YourUsername>\\AppData\\Local\\LocalWhisper\\models\\ggml-small.bin"
language = "de"  # or "en"
timeout_seconds = 60
```

### Update Post-Processing paths (if enabled):

```toml
[postprocessing]
enabled = false  # Set to true to enable post-processing
llm_cli_path = "C:\\Users\\<YourUsername>\\AppData\\Local\\LocalWhisper\\bin\\llama-cli.exe"
model_path = "C:\\Users\\<YourUsername>\\AppData\\Local\\LocalWhisper\\models\\Llama-3.2-3B-Instruct-Q4_K_M.gguf"
timeout_seconds = 5
gpu_acceleration = true  # Set to false if using CPU-only build
use_glossary = false
glossary_path = ""
temperature = 0.0
top_p = 0.25
repeat_penalty = 1.05
max_tokens = 512
```

**Important Notes:**
- Use double backslashes (`\\`) in Windows paths
- Replace `<YourUsername>` with your actual Windows username
- Ensure all paths point to existing files

## Step 9: Restart LocalWhisper

1. Close LocalWhisper (right-click tray icon → "Beenden")
2. Launch LocalWhisper.exe again
3. The app should now load with correct paths

## Step 10: Test the Application

1. Open any text editor (e.g., Notepad)
2. Press your configured hotkey (default: Ctrl+Shift+A)
3. Speak into your microphone for ~500ms
4. The transcript should appear in your clipboard
5. Paste (Ctrl+V) to see the result

## Troubleshooting

### Error: "Whisper-Modell nicht gefunden"
- Verify `whisper.cli_path` points to correct `whisper-cli.exe` location
- Verify `whisper.model_path` points to correct `.bin` file
- Ensure paths use double backslashes (`\\`)

### Error: "DLL not found" or Exit Code -1073741515
- Ensure all DLL files from `whisper-bin-x64.zip` are in the `bin/` folder
- Required: `whisper.dll`, `SDL2.dll`, all `ggml*.dll` files

### Error: "LLM CLI path must be specified"
- If you don't want post-processing, set `postprocessing.enabled = false` in config.toml
- If you do want it, ensure `llm_cli_path` and `model_path` are correctly set

### Hotkey conflict warning on startup
- The configured hotkey is already in use by another application
- Go to Settings (right-click tray icon → "Einstellungen")
- Choose a different hotkey combination
- Click "Speichern und neu starten"

### No microphone detected
- Check Windows Sound Settings
- Ensure a microphone is connected and enabled
- Grant microphone permissions to LocalWhisper

## Portable Deployment

This setup is **fully portable** and does not require:
- Administrator rights
- Visual C++ Redistributable installation
- Registry modifications
- System-wide dependencies

You can copy the entire `LocalWhisper/` folder to another machine and it will work (assuming same Windows version and hardware).

## File Size Reference

Typical installation sizes:
- LocalWhisper.exe: ~1 MB
- Whisper CLI + DLLs: ~50 MB
- Llama CLI: ~50-100 MB (depends on CUDA version)
- Whisper small model: ~466 MB
- Llama 3.2 3B Q4_K_M model: ~1.87 GB

**Total:** ~2.5-3 GB (with post-processing enabled)

## Links

- **LocalWhisper Repository:** [GitHub](https://github.com/kschlt/LocalWhisper)
- **Whisper.cpp:** https://github.com/ggerganov/whisper.cpp
- **Llama.cpp:** https://github.com/ggerganov/llama.cpp
- **Whisper Models (HuggingFace):** https://huggingface.co/ggerganov/whisper.cpp
- **Llama Models (HuggingFace):** https://huggingface.co/models?search=llama+gguf

## Next Steps

- Configure post-processing settings in Settings UI
- Download additional language models if needed
- Set up glossary for domain-specific terms
- Explore history viewer (right-click tray icon → "Verlauf anzeigen")

---

**Last Updated:** 2025-12-05
**Version:** 0.1.0
