# Whisper Model Reference

**Purpose:** Complete reference for supported Whisper.cpp models

**Source:** Official whisper.cpp repository and OpenAI research
**Last Updated:** 2025-11-17

---

## Supported Models

LocalWhisper supports **base, small, medium, and large-v3** models in both multilingual and English-only variants.

**Total:** 8 models (4 sizes × 2 language variants)

---

## Model Specifications

### Base Models

| Model | Size | SHA-1 Hash | Download URL |
|-------|------|-----------|--------------|
| base | 142 MiB | `465707469ff3a37a2b9b8d8f89f2f99de7299dac` | [HuggingFace](https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin) |
| base.en | 142 MiB | `137c40403d78fd54d454da0f9bd998f78703390c` | [HuggingFace](https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.en.bin) |

**Parameters:** 74M
**VRAM:** ~1 GB
**Speed:** ~7x faster than large
**Use Case:** Quick dictation, real-time transcription

### Small Models (Recommended)

| Model | Size | SHA-1 Hash | Download URL |
|-------|------|-----------|--------------|
| small | 466 MiB | `55356645c2b361a969dfd0ef2c5a50d530afd8d5` | [HuggingFace](https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin) |
| small.en | 466 MiB | `db8a495a91d927739e50b3fc1cc4c6b8f6c2d022` | [HuggingFace](https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.en.bin) |

**Parameters:** 244M
**VRAM:** ~2 GB
**Speed:** ~4x faster than large
**Use Case:** ⭐ **Best balance** - recommended for most users

### Medium Models

| Model | Size | SHA-1 Hash | Download URL |
|-------|------|-----------|--------------|
| medium | 1.5 GiB | `fd9727b6e1217c2f614f9b698455c4ffd82463b4` | [HuggingFace](https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-medium.bin) |
| medium.en | 1.5 GiB | `8c30f0e44ce9560643ebd10bbe50cd20eafd3723` | [HuggingFace](https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-medium.en.bin) |

**Parameters:** 769M
**VRAM:** ~5 GB
**Speed:** ~2x faster than large
**Use Case:** Higher quality transcription, acceptable speed

### Large Models

| Model | Size | SHA-1 Hash | Download URL |
|-------|------|-----------|--------------|
| large-v3 | 2.9 GiB | `ad82bf6a9043ceed055076d0fd39f5f186ff8062` | [HuggingFace](https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v3.bin) |

**Parameters:** 1550M
**VRAM:** ~10 GB
**Speed:** 1x (baseline)
**Use Case:** Highest quality, slowest processing

**Note:** Large models are multilingual only (no .en variant in whisper.cpp)

---

## Hash Verification

**Algorithm:** SHA-1 (NOT SHA-256)

**Why SHA-1?** Whisper.cpp uses SHA-1 for model verification. While SHA-1 is deprecated for cryptographic purposes, it's sufficient for file integrity checking.

**C# Implementation:**
```csharp
using var sha1 = System.Security.Cryptography.SHA1.Create();
using var stream = File.OpenRead(filePath);

var hashBytes = await sha1.ComputeHashAsync(stream);
var computedHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

// Compare: computedHash == expectedHash
```

---

## Model Comparison

### Performance Characteristics

| Model | Parameters | Speed Factor | VRAM | Relative Accuracy |
|-------|-----------|--------------|------|-------------------|
| base | 74M | 7x | ~1 GB | Good |
| small | 244M | 4x | ~2 GB | Better |
| medium | 769M | 2x | ~5 GB | High |
| large-v3 | 1550M | 1x | ~10 GB | Highest |

**Speed Factor:** Relative to large model on A100 GPU (measured by OpenAI)

### Language-Specific Recommendations

**German (de):**
- **Quick dictation:** base or small
- **Best balance:** small ⭐
- **High quality:** medium or large-v3

**English (en):**
- **Quick dictation:** base.en or small.en
- **Best balance:** small.en ⭐
- **High quality:** medium.en or large-v3

**Why .en models?** English-only models perform slightly better for English than multilingual models of the same size.

### UI Display Strings (German)

**For Wizard Model Selection:**

| Model | Display String |
|-------|----------------|
| base | `Schnell (142 MB) - Gut für Echtzeit` |
| small | `Empfohlen (466 MB) - Beste Balance ⭐` |
| medium | `Hohe Qualität (1.5 GB) - Langsamer` |
| large-v3 | `Höchste Qualität (2.9 GB) - Am langsamsten` |

---

## Configuration Format (TOML)

**File:** `config/config.toml`

```toml
[[whisper.available_models]]
name = "base"
filename = "ggml-base.bin"
size_mb = 142
sha1 = "465707469ff3a37a2b9b8d8f89f2f99de7299dac"
download_url = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin"
speed_factor = 7.0
vram_gb = 1
description = "Schnell (142 MB) - Gut für Echtzeit"

[[whisper.available_models]]
name = "base.en"
filename = "ggml-base.en.bin"
size_mb = 142
sha1 = "137c40403d78fd54d454da0f9bd998f78703390c"
download_url = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.en.bin"
speed_factor = 7.0
vram_gb = 1
description = "Fast (142 MB) - Good for real-time"

[[whisper.available_models]]
name = "small"
filename = "ggml-small.bin"
size_mb = 466
sha1 = "55356645c2b361a969dfd0ef2c5a50d530afd8d5"
download_url = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin"
speed_factor = 4.0
vram_gb = 2
description = "Empfohlen (466 MB) - Beste Balance ⭐"

[[whisper.available_models]]
name = "small.en"
filename = "ggml-small.en.bin"
size_mb = 466
sha1 = "db8a495a91d927739e50b3fc1cc4c6b8f6c2d022"
download_url = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.en.bin"
speed_factor = 4.0
vram_gb = 2
description = "Recommended (466 MB) - Best balance ⭐"

[[whisper.available_models]]
name = "medium"
filename = "ggml-medium.bin"
size_mb = 1536
sha1 = "fd9727b6e1217c2f614f9b698455c4ffd82463b4"
download_url = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-medium.bin"
speed_factor = 2.0
vram_gb = 5
description = "Hohe Qualität (1.5 GB) - Langsamer"

[[whisper.available_models]]
name = "medium.en"
filename = "ggml-medium.en.bin"
size_mb = 1536
sha1 = "8c30f0e44ce9560643ebd10bbe50cd20eafd3723"
download_url = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-medium.en.bin"
speed_factor = 2.0
vram_gb = 5
description = "High quality (1.5 GB) - Slower"

[[whisper.available_models]]
name = "large-v3"
filename = "ggml-large-v3.bin"
size_mb = 2960
sha1 = "ad82bf6a9043ceed055076d0fd39f5f186ff8062"
download_url = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v3.bin"
speed_factor = 1.0
vram_gb = 10
description = "Höchste Qualität (2.9 GB) - Am langsamsten"
```

---

## Download Instructions (Manual)

**For Iteration 5a** (before HTTP download is implemented):

### Option 1: Direct Download from HuggingFace

Visit: `https://huggingface.co/ggerganov/whisper.cpp/tree/main`

Download the desired model:
- Click on model file (e.g., `ggml-small.bin`)
- Click "Download" button
- Save to a temporary location

### Option 2: Using whisper.cpp Download Script

**Requirements:** Bash, wget or curl

```bash
# Clone whisper.cpp repository
git clone https://github.com/ggml-org/whisper.cpp.git
cd whisper.cpp/models

# Download specific model
./download-ggml-model.sh small

# Model saved to: models/ggml-small.bin
```

### Option 3: Direct URL Download

**Using curl:**
```bash
curl -L -o ggml-small.bin https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin
```

**Using wget:**
```bash
wget https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin
```

---

## Verification After Download

**Check file size:**
```bash
# Windows PowerShell
(Get-Item ggml-small.bin).Length / 1MB
# Should be ~466 MB

# Linux/Mac
ls -lh ggml-small.bin
```

**Compute SHA-1 hash:**

**Windows PowerShell:**
```powershell
Get-FileHash -Algorithm SHA1 ggml-small.bin
# Compare to: 55356645c2b361a969dfd0ef2c5a50d530afd8d5
```

**Linux/Mac:**
```bash
sha1sum ggml-small.bin
# Compare to: 55356645c2b361a969dfd0ef2c5a50d530afd8d5
```

---

## Model Selection Guidelines

### For Most Users
→ **small** (multilingual) or **small.en** (English-only)
- Best balance of speed and quality
- Works well for typical dictation (< 30 seconds)
- Low VRAM requirements (~2 GB)

### For Real-Time Applications
→ **base** or **base.en**
- 7x faster than large
- Acceptable quality for clean audio
- Minimal VRAM (~1 GB)

### For High-Quality Transcription
→ **medium** or **large-v3**
- Better for difficult audio (accents, background noise)
- Slower processing (2x or 1x)
- Higher VRAM requirements (5-10 GB)

### For Multilingual Support
→ Use models without `.en` suffix
- Supports 99+ languages
- Slightly lower accuracy for English compared to .en models

---

## Troubleshooting

### Hash Mismatch After Download
**Cause:** Incomplete or corrupted download
**Solution:** Delete file and re-download

### Model File Too Small
**Cause:** Downloaded HTML error page instead of binary
**Solution:** Check URL, ensure using direct download link with `/resolve/` path

### "Model not found" Error
**Cause:** Incorrect path in config.toml
**Solution:** Verify `whisper.model_path` points to actual .bin file location

### Slow Transcription
**Cause:** Model too large for hardware
**Solution:** Try smaller model (e.g., medium → small → base)

---

## Future Models (Not Yet Supported)

**tiny / tiny.en** - Not recommended (75 MiB)
- Lowest quality, marginal speed improvement over base
- Not worth the accuracy trade-off for desktop application

**large-v1 / large-v2** - Superseded by large-v3
- Older versions, no advantage over v3

**turbo** - New optimized model (809M parameters, 8x speed)
- Not yet widely tested with whisper.cpp
- Consider for future iterations if stable

---

## References

- [Whisper.cpp Repository](https://github.com/ggml-org/whisper.cpp)
- [Whisper.cpp Models README](https://github.com/ggml-org/whisper.cpp/blob/master/models/README.md)
- [OpenAI Whisper Paper](https://cdn.openai.com/papers/whisper.pdf)
- [HuggingFace Model Hub](https://huggingface.co/ggerganov/whisper.cpp/tree/main)

---

**Document Version:** 1.0
**Applies To:** LocalWhisper Iteration 5a, 5b
