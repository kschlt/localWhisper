# Iteration 7: Optional Post-Processing

**Purpose:** Add optional LLM-based post-processing to improve transcript formatting while preserving meaning
**Status:** 📋 Planning / Questions Phase
**User Stories:** US-060, US-061, US-062
**Functional Requirements:** FR-022
**Estimated Effort:** 4-6 hours (implementation), 1-2 hours (model setup/testing)

---

## Overview

After Whisper transcription, optionally run a local small LLM to:
- Clean up grammar, punctuation, capitalization
- Add paragraph breaks between distinct topics
- Convert spoken lists to bullet/numbered lists
- Fix filler words ("uh", "um", "like")
- **Never change meaning** - formatting only

**Critical:** Must be **fast** (ideally <1s) and **local** (no cloud API).

---

## Existing Specifications

### FR-022 Requirements

From `docs/specification/functional-requirements.md:332-348`:

- ✓ Settings toggle: "Post-Processing aktivieren" (default: off)
- ✓ When enabled: invoke `llm-cli.exe` with transcript via stdin
- ✓ Prompt: "Format lists, apply glossary, fix punctuation. Do not change meaning."
- ✓ LLM output replaces STT output for clipboard + history
- ✓ **Fallback on error:** If LLM fails/times out → use original STT text
- ✓ Post-processing latency logged
- ✓ Optional custom glossary file (`config/glossary.txt`)

### Existing User Stories

**US-060:** Enable/disable in Settings
**US-061:** Fallback on error (10s timeout)
**US-062:** Meaning preservation contract

See: `docs/specification/user-stories-gherkin.md:1337-1414`

---

## New Research: Llama 3.2 3B Dual-Prompt Approach

Based on user research, we're refining the implementation:

### Model Selection

**Llama 3.2 3B Instruct** (GGUF format)
- Size: ~2GB (Q4_K_M quantization)
- Speed: Fast enough for near-instant responses on modern CPUs
- Quality: Excellent for formatting/cleanup tasks
- Availability: Free, permissive license

### Two-Mode Operation

**Mode 1: Plain Text (Default - 90% of use)**
- Clean grammar, punctuation, capitalization
- Add paragraph breaks
- Simple lists with `- ` or `1. `
- **No Markdown** headings/bold/italics

**Mode 2: Markdown (Explicit)**
- Same as plain text, PLUS:
- Markdown headings (`## Heading`)
- Proper Markdown list formatting
- Triggered by user saying "markdown mode" at start/end of dictation

### Trigger Word Detection

**Before sending to LLM:**
1. Check if transcript starts/ends with "markdown mode" (case-insensitive)
2. If found: Strip trigger phrase + use Markdown prompt
3. Else: Use Plain Text prompt

### Model Parameters

```
temperature = 0 (deterministic)
top_p = 0.2-0.3
max_tokens ≈ input_length * 1.2
repeat_penalty = 1.05-1.1
```

### Prompts

See **Appendix A** for full prompt text (plain + markdown modes).

---

## Critical Questions to Resolve

### 🔴 HIGH PRIORITY (Blocking)

#### Q1: **Model Execution Engine**

How do we run Llama 3.2 3B on Windows locally?

**Options:**
- **A) llama.cpp** (`llama-cli.exe`) - Standalone CLI, cross-platform, well-tested
- **B) Ollama** - Model server, requires background process, more overhead
- **C) LlamaFile** - Single executable with model embedded, ~2GB file

**Questions:**
- Which is fastest for our use case (single synchronous inference)?
- Which has smallest footprint (memory/disk)?
- Which is easiest to distribute with the app?
- Can we bundle it, or require user to download separately?

**User preference:** ? (Need decision)

---

#### Q2: **CPU vs GPU Acceleration**

**Context:**
- User emphasized: "Speed is critical - must feel instant"
- Llama 3.2 3B on CPU (modern Intel/AMD) ≈ 1-3s for ~100 tokens
- GPU (CUDA/DirectML) ≈ 0.2-0.5s for ~100 tokens

**Questions:**
- Do we require GPU? (Limits audience to NVIDIA GPUs)
- Do we support DirectML (AMD/Intel GPUs on Windows)?
- Do we fall back to CPU if no GPU detected?
- What's the acceptable latency threshold? (500ms? 1s? 2s?)

**Performance targets:**
- Ideal: <500ms (feels instant)
- Good: <1s (acceptable)
- Max: <2s (tolerable for formatting)

**User preference:** ? (Need decision)

---

#### Q3: **Model Storage & Distribution**

**Where does the model file live?**

**Options:**
- **A) Data Root:** `<DATA_ROOT>/models/llama-3.2-3b-q4.gguf` (~2GB)
  - Pro: Centralized with Whisper models
  - Con: Data root migrations copy huge files
- **B) App Directory:** `C:\Program Files\LocalWhisper\models\` (read-only)
  - Pro: Model is part of app installation
  - Con: Requires admin for updates
- **C) User Profile:** `%APPDATA%\LocalWhisper\models\`
  - Pro: No admin needed
  - Con: Separate from data root

**Distribution:**
- Do we bundle the 2GB model with the installer? (Installer becomes 2.5GB+)
- Do we auto-download on first use? (Requires internet)
- Do we require user to manually download? (Friction)

**User preference:** ? (Need decision)

---

#### Q4: **Trigger Word Implementation**

**How do we detect "markdown mode"?**

**Proposed:**
```
Before LLM call:
1. transcript_lower = transcript.ToLower().Trim()
2. If transcript_lower starts with "markdown mode" OR ends with "markdown mode":
   - Strip the trigger phrase
   - Use Markdown prompt
3. Else:
   - Use Plain Text prompt
```

**Questions:**
- Is "markdown mode" the only trigger? Or support aliases ("markdown", "md mode")?
- Should we support German ("Markdown-Modus")?
- Should trigger removal preserve punctuation? ("markdown mode. Let's discuss..." → "Let's discuss...")
- Should we log which mode was used?

**User preference:** "markdown mode" at start/end is enough? Or want more flexibility?

---

### 🟡 MEDIUM PRIORITY (Can decide during implementation)

#### Q5: **Settings UI**

**What goes in Settings window?**

**Proposed:**
```
[ ] Post-Processing aktivieren
    LLM-Pfad: [C:\Tools\llama-cli.exe] [Browse...]
    Modell-Pfad: [<DATA_ROOT>\models\llama-3.2-3b-q4.gguf] [Browse...]

    [x] GPU-Beschleunigung (falls verfügbar)
    [ ] Benutzerdefiniertes Glossar verwenden
        Glossar-Pfad: [<DATA_ROOT>\config\glossary.txt] [Browse...]
```

**Questions:**
- Do we expose temperature/top_p/max_tokens? (Advanced users only?)
- Do we auto-detect LLM CLI path? (Search common locations?)
- Do we validate model file on Save? (Check file exists + size >100MB?)

---

#### Q6: **Glossary Feature**

**FR-022 mentions custom glossary** (`config/glossary.txt`). How does this work?

**Proposed Format:**
```
# glossary.txt - Abbreviation expansion
asap = as soon as possible
fyi = for your information
imho = in my humble opinion
```

**Implementation:**
- Pre-process transcript: Replace abbreviations BEFORE sending to LLM
- OR: Include glossary in LLM prompt context

**Questions:**
- Which approach is better? (Pre-process vs LLM-aware)
- Do we validate glossary format on load?
- Max glossary size? (100 entries? 500?)

**Priority:** Low (can defer to post-v1.0)

---

#### Q7: **Fallback Strategy**

**When does fallback occur?**

**Triggers:**
- LLM process exits with non-zero code
- Timeout exceeded (10s default)
- Output is empty/invalid
- Model file not found
- Out of memory

**Behavior:**
- Use original STT text (guaranteed to have data)
- Show warning flyout: "⚠ Post-Processing fehlgeschlagen (Original-Text verwendet)"
- Log error with details

**Questions:**
- Should we retry once before fallback?
- Should we disable post-processing automatically after X consecutive failures?
- Should we show different messages for different errors? (timeout vs crash vs missing model)

---

### 🟢 LOW PRIORITY (Nice to have)

#### Q8: **Performance Monitoring**

**What do we log/measure?**

**Proposed:**
```
[INFO] PostProcessor: Started (mode: PlainText, input_length: 87)
[INFO] PostProcessor: Completed in 1.234s (output_length: 92)
```

**Questions:**
- Do we track p50/p95/p99 latency like we do for STT?
- Do we expose metrics in Settings? ("Average post-processing time: 1.2s")
- Do we warn if consistently slow? (">2s average - consider disabling or using GPU")

---

#### Q9: **Multi-Language Support**

**Current:** Prompts are in English (model follows instructions well)
**Future:** Should we localize prompts to German if user's language is "de"?

**Impact:** Minimal - LLMs follow English instructions regardless of input language
**Decision:** Defer to post-v1.0

---

## Implementation Plan (DRAFT)

**Order of work:**

### Phase 1: Foundation (~2h)
1. Create `LlmPostProcessor` service
2. Implement CLI subprocess execution (similar to `WhisperCLIAdapter`)
3. Implement trigger word detection
4. Load plain/markdown prompts from config or constants

### Phase 2: Integration (~1-2h)
5. Update `StateMachine`: Add `PostProcessing` state after `Processing`
6. Wire post-processor into dictation flow
7. Implement fallback logic

### Phase 3: Settings UI (~1h)
8. Add post-processing section to `SettingsWindow`
9. Save/load config (LLM path, model path, enabled flag)

### Phase 4: Testing (~1h)
10. Test with Llama 3.2 3B model
11. Measure latency (CPU vs GPU)
12. Test fallback scenarios
13. Validate prompts produce expected output

### Phase 5: Documentation (~30min)
14. Update README with post-processing setup
15. Document how to download/configure Llama 3.2 3B
16. Add troubleshooting guide

**Total: ~5-6 hours**

---

## Prompts (Appendix A)

### Plain Text Mode (Default)

```
System Prompt:

You are a careful transcript formatter and light copy editor for my personal notes, prompts, and emails.

INPUT:
- Raw text produced by an automatic speech recognition system (Whisper).
- It may contain long run-on sentences, missing punctuation, and no paragraph breaks.

YOUR GOAL:
- Make the text easy to read while preserving my intent and personality.

DO:
- Fix grammar, punctuation, and capitalization.
- Split very long sentences when it clearly improves clarity.
- Insert paragraph breaks between distinct topics or when there is an obvious transition
  (for example: "first…", "second…", "another thing…", "on a different note…").
- Turn clearly spoken enumerations into simple lists, but only when it is obviously helpful:
  - Typical cues: "first", "second", "third", "the main points are…", "benefits are…", "downsides are…"
  - Each list item should be a short sentence or phrase that stands on its own.
- Remove obvious filler words ("uh", "um", "you know", "like" used as filler) when this does not change the meaning.
- Keep my tone and character as much as possible.

DON'T:
- Don't add new ideas, explanations, or examples.
- Don't change the meaning of what I said.
- Don't summarize or shorten important content.
- Don't change technical terms, names, or concrete details.
- Don't use Markdown headings, bold, italics, code blocks, or tables.

OUTPUT:
- Plain text only.
- Use blank lines between paragraphs.
- When you use a list, format it as simple lines starting with "- " for bullets or "1. ", "2. ", etc. for numbered lists.
- Do not add any commentary or explanation.
- Return only the cleaned and formatted text.
```

### Markdown Mode (Explicit)

```
System Prompt:

You are a formatter that converts transcribed speech into clean Markdown.

INPUT:
- Raw text produced by an automatic speech recognition system (Whisper).
- It may contain long run-on sentences, missing punctuation, and no paragraph breaks.

GOAL:
- Make the text easy to read and well-structured Markdown while preserving my intent and personality.

DO:
- Fix grammar, punctuation, and capitalization.
- Split very long sentences when it clearly improves clarity.
- Use blank lines between paragraphs.
- Use bullet lists ("- item") or numbered lists ("1. item") for clearly spoken lists.
- Use short Markdown headings (## Heading) only when I clearly change topic
  (for example: "now the benefits are…", "the downsides are…", "next I want to talk about…").

DON'T:
- Don't add new ideas, explanations, or examples.
- Don't change the meaning.
- Don't summarize or shorten important content.

OUTPUT:
- Valid Markdown only.
- No commentary or explanations.
- Do not include the words "markdown" or "heading" in the output unless they are part of my original text.
```

---

## Example Workflow

**User dictates:**
> "markdown mode. Okay, I want three sections. First, what the feature does. Second, why it's useful. And third, what the limitations are. The feature takes Whisper transcripts and cleans them up. Then it's useful because it makes everything easier to read and saves me time. And the limitations are that it might still slightly change wording and it's not meant for legal documents."

**Processing:**
1. Whisper STT → raw transcript (above)
2. Trigger detection: Starts with "markdown mode" → **Markdown prompt**
3. Strip trigger: "Okay, I want three sections..."
4. Send to LLM with Markdown prompt
5. LLM output:
```markdown
## What the feature does

The feature takes Whisper transcripts and cleans them up.

## Why it's useful

It makes everything easier to read and saves me time.

## Limitations

- It might still slightly change wording.
- It is not meant for legal documents.
```
6. Write to clipboard + history (formatted)
7. Show flyout: "✓ Transkription formatiert (Markdown-Modus)"

---

## Questions Summary (For User)

**🔴 Critical decisions needed before implementation:**

1. **Model execution engine:** llama.cpp, Ollama, or LlamaFile?
2. **CPU vs GPU:** Require GPU for speed? Or CPU-only? Or support both?
3. **Model storage:** Data root, app directory, or user profile?
4. **Model distribution:** Bundle with installer, auto-download, or manual?
5. **Trigger words:** Just "markdown mode"? Or support aliases/German?
6. **Performance target:** What latency is "instant"? 500ms? 1s? 2s?

**🟡 Can decide later:**
7. Settings UI: Expose advanced params (temp, top_p)?
8. Glossary: Pre-process or LLM-aware? Defer to v1.1?
9. Fallback: Retry logic? Auto-disable after failures?

**🟢 Nice to have:**
10. Performance monitoring: Track p95 latency?
11. Multi-language prompts: Localize to German?

---

## Next Steps

1. **User answers critical questions** (Q1-Q6)
2. **Create ADR** for chosen model execution approach
3. **Update user stories** with concrete implementation details
4. **Write BDD scenarios** for plain/markdown modes
5. **Begin implementation** (when GitHub is back + branches merged)

---

**Last Updated:** 2025-11-18
**Status:** Awaiting user decisions on critical questions
