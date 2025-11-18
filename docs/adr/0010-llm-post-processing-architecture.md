# ADR-0010: LLM Post-Processing Architecture

**Status:** ✅ Accepted
**Date:** 2025-11-18
**Deciders:** Product Owner, Technical Lead
**Related:** FR-022, US-060, US-061, US-062, ADR-0002 (CLI subprocess approach)

---

## Context

After Whisper STT transcription, users often receive raw text with:
- Missing punctuation and capitalization
- No paragraph breaks
- Run-on sentences
- Filler words ("uh", "um", "like")
- Spoken lists not formatted clearly

We want to add **optional** post-processing to improve formatting while:
- ✅ Preserving meaning (never hallucinate or change intent)
- ✅ Running **locally** (no cloud APIs, privacy-first)
- ✅ Being **fast** (<1s ideal, must feel instant)
- ✅ Falling back gracefully on errors

---

## Decision

We will implement **optional LLM-based post-processing** using:

### **Model: Llama 3.2 3B Instruct (GGUF, Q4_K_M quantization)**

**Rationale:**
- Small enough for fast inference (~1-2s on CPU, <500ms on GPU)
- High quality for formatting tasks
- Free, permissive license (Apache 2.0)
- Excellent instruction-following for "format but don't change meaning" prompts

**Size:** ~2GB (Q4_K_M quantization balances quality and speed)

---

### **Execution Engine: llama.cpp (`llama-cli.exe`)**

**Why llama.cpp:**
- ✅ Simple CLI (same pattern as Whisper CLI - ADR-0002)
- ✅ Cross-platform (Windows, macOS, Linux)
- ✅ Well-tested and actively maintained
- ✅ Supports GPU acceleration (CUDA, Metal, DirectML)
- ✅ No background server needed (unlike Ollama)
- ✅ Synchronous execution (perfect for our use case)

**Alternatives considered:**
- **Ollama:** Requires background server (unnecessary overhead)
- **LlamaFile:** Embeds model in 2GB executable (harder to update model separately)

---

### **GPU Acceleration: Use fastest available, CPU fallback**

**Strategy:**
1. Detect GPU capabilities on startup (CUDA/DirectML)
2. If GPU available: Use GPU-accelerated inference (~200-500ms)
3. If no GPU or GPU fails: Fall back to CPU (~1-3s)
4. App works everywhere, but faster on GPU-equipped machines

**Why this approach:**
- Maximizes performance for users with GPUs
- Maintains compatibility for users without GPUs
- No user configuration needed (automatic detection)

**DirectML on Windows:**
- Supports AMD, Intel, NVIDIA GPUs
- Requires llama.cpp built with `LLAMA_CUDA=1` or `LLAMA_CLBLAST=1`
- Falls back to CPU if DirectML initialization fails

---

### **Model Storage: `<DATA_ROOT>/models/llama-3.2-3b-q4.gguf`**

**Why same location as Whisper models:**
- ✅ Centralized model management
- ✅ User already chose this location in first-run wizard
- ✅ Consistent with existing architecture
- ✅ Easy to find and manage (all models in one place)

**Path resolution:**
```
<DATA_ROOT>/models/llama-3.2-3b-q4.gguf
```

Example: `C:\Users\Alice\LocalWhisper\models\llama-3.2-3b-q4.gguf`

---

### **Model Distribution: Auto-download (like Whisper wizard)**

**Flow:**
1. User enables post-processing in Settings
2. If model not found: Show download dialog
3. Auto-download from Hugging Face (or mirror)
4. Verify SHA-256 hash after download
5. Ready to use

**Why auto-download:**
- ✅ No manual steps for users
- ✅ Consistent with Whisper model download in wizard
- ✅ Ensures correct model version
- ✅ Can update model by re-downloading

**Download source:** Hugging Face Model Hub
**URL:** `https://huggingface.co/bartowski/Llama-3.2-3B-Instruct-GGUF/resolve/main/Llama-3.2-3B-Instruct-Q4_K_M.gguf`

**Fallback:** Provide manual download instructions if auto-download fails

---

### **Dual-Prompt Mode: Plain Text (default) + Markdown (explicit)**

**Mode 1: Plain Text (90% of use cases)**
- Clean grammar, punctuation, capitalization
- Add paragraph breaks
- Simple lists (`- item` or `1. item`)
- **No Markdown** headings/bold/italics

**Mode 2: Markdown (explicit trigger)**
- Same as plain text, PLUS:
- Markdown headings (`## Heading`)
- Proper Markdown formatting
- Triggered by "markdown mode" in transcript

**Why two modes:**
- Most transcripts are notes/prompts/emails → plain text is cleaner
- Markdown mode available when needed (documentation, structured notes)
- Clear separation prevents unwanted Markdown formatting

---

### **Trigger Detection: Regex scan for "markdown mode"**

**Algorithm:**
```
Before LLM invocation:
1. Extract first ~20 words and last ~20 words of transcript
2. Check if "markdown mode" appears (case-insensitive)
3. If found:
   - Strip trigger phrase (preserving punctuation)
   - Use Markdown prompt
4. Else:
   - Use Plain Text prompt
```

**Regex pattern:**
```csharp
Regex.IsMatch(firstWords + " " + lastWords, @"\bmarkdown\s+mode\b", RegexOptions.IgnoreCase)
```

**Examples:**
- ✅ "markdown mode. Let's discuss the architecture..." → Markdown
- ✅ "Let's discuss the architecture. markdown mode" → Markdown
- ✅ "MARKDOWN MODE here are three sections..." → Markdown
- ❌ "Let's markdown this mode of thinking" → Plain Text (not exact phrase)

**Why first/last ~20 words:**
- Covers natural speaking patterns ("At the beginning: markdown mode...")
- Avoids false positives mid-transcript
- Fast to check (no full-text scan)

---

### **Performance Targets**

**Latency:**
- **Ideal:** <500ms (feels instant)
- **Target:** <1s (acceptable for most users)
- **Acceptable:** <2s (noticeable but tolerable)
- **Timeout:** 5s (beyond this, fallback to original text)

**Why these targets:**
- Post-processing should be faster than STT (1-3s)
- If slower than STT, users will perceive it as "extra wait"
- 5s timeout prevents indefinite hangs

**Measuring performance:**
- Log p50, p95, p99 latency for post-processing
- Warn user if consistently >2s ("Consider using GPU or disabling")
- Display avg latency in Settings (optional)

---

### **Fallback Strategy: Always preserve original text**

**Fallback triggers:**
1. LLM process exits with non-zero code
2. Timeout exceeded (5s)
3. Output is empty or invalid
4. Model file not found
5. Out of memory / GPU error

**Behavior on fallback:**
1. Use original STT text (guaranteed data)
2. Write to clipboard and history (as if post-processing disabled)
3. Show warning flyout: "⚠ Post-Processing fehlgeschlagen (Original-Text verwendet)"
4. Log error with details (exit code, stderr, duration)

**Why this is critical:**
- User's data is never lost
- App remains usable even if post-processing fails
- Graceful degradation (feature enhancement, not core functionality)

---

## CLI Interface

### **Invocation**

```bash
llama-cli.exe \
  --model "<DATA_ROOT>\models\llama-3.2-3b-q4.gguf" \
  --prompt "System: <PROMPT_TEXT>\n\nUser: <TRANSCRIPT>\n\nAssistant:" \
  --n-predict 512 \
  --temp 0.0 \
  --top-p 0.25 \
  --repeat-penalty 1.05 \
  --threads <CPU_CORES> \
  --n-gpu-layers 99 \
  --quiet
```

**Parameters:**
- `--model`: Path to GGUF model file
- `--prompt`: System prompt + user transcript
- `--n-predict`: Max tokens to generate (~1.2x input length)
- `--temp 0.0`: Deterministic output (no randomness)
- `--top-p 0.25`: Low diversity (formatting is deterministic)
- `--repeat-penalty 1.05`: Avoid repetitive phrases
- `--threads`: Use all CPU cores for speed
- `--n-gpu-layers 99`: Offload all layers to GPU (if available)
- `--quiet`: Suppress progress output

**Input/Output:**
- **stdin:** Transcript text
- **stdout:** Formatted text
- **stderr:** Error messages (if any)
- **Exit code:** 0 = success, non-zero = error

---

### **Prompt Templates**

**Plain Text Mode (default):**
```
System: You are a careful transcript formatter and light copy editor.

INPUT: Raw text from speech recognition (Whisper). May contain run-on sentences, missing punctuation.

YOUR GOAL: Make text easy to read while preserving intent and personality.

DO:
- Fix grammar, punctuation, capitalization.
- Split long sentences when it improves clarity.
- Insert paragraph breaks between distinct topics.
- Turn clearly spoken lists into simple bullets (- item) or numbers (1. item).
- Remove filler words ("uh", "um", "like") when safe.

DON'T:
- Don't add new ideas or explanations.
- Don't change meaning.
- Don't summarize or shorten.
- Don't change technical terms or names.
- Don't use Markdown headings, bold, italics.

OUTPUT: Plain text only. Blank lines between paragraphs. Simple lists only.

User: <TRANSCRIPT>