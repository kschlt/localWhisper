namespace LocalWhisper.Models;

/// <summary>
/// Post-Processing configuration section (Iteration 7).
/// Configures LLM-based transcript post-processing via llama.cpp.
/// </summary>
/// <remarks>
/// See: ADR-0010 (LLM Post-Processing Architecture)
/// See: US-060, US-061, US-062, US-063, US-064
/// </remarks>
public class PostProcessingConfig
{
    private int _timeoutSeconds = 5;

    /// <summary>
    /// Enable post-processing.
    /// Default: false (disabled).
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Path to llama-cli.exe executable.
    /// Default: Empty (must be configured before first use).
    /// </summary>
    public string LlmCliPath { get; set; } = string.Empty;

    /// <summary>
    /// Path to Llama model file (e.g., llama-3.2-3b-q4.gguf).
    /// Default: Empty (must be configured before first use).
    /// </summary>
    public string ModelPath { get; set; } = string.Empty;

    /// <summary>
    /// Timeout for LLM processing in seconds.
    /// Valid range: 1-30 seconds.
    /// Default: 5 seconds.
    /// </summary>
    public int TimeoutSeconds
    {
        get => _timeoutSeconds;
        set
        {
            if (value < 1 || value > 30)
            {
                throw new ArgumentException("Timeout must be between 1 and 30 seconds");
            }
            _timeoutSeconds = value;
        }
    }

    /// <summary>
    /// Enable GPU acceleration (CUDA/DirectML/Metal).
    /// Default: true (enabled).
    /// </summary>
    public bool GpuAcceleration { get; set; } = true;

    /// <summary>
    /// Enable glossary support.
    /// Default: false (disabled).
    /// </summary>
    public bool UseGlossary { get; set; } = false;

    /// <summary>
    /// Path to glossary file.
    /// Default: Empty.
    /// </summary>
    public string GlossaryPath { get; set; } = string.Empty;

    /// <summary>
    /// LLM temperature (0.0 = deterministic).
    /// Default: 0.0.
    /// </summary>
    public float Temperature { get; set; } = 0.0f;

    /// <summary>
    /// LLM top-p sampling (nucleus sampling).
    /// Default: 0.25 (low diversity for formatting tasks).
    /// </summary>
    public float TopP { get; set; } = 0.25f;

    /// <summary>
    /// LLM repeat penalty.
    /// Default: 1.05.
    /// </summary>
    public float RepeatPenalty { get; set; } = 1.05f;

    /// <summary>
    /// Maximum tokens to generate.
    /// Default: 512.
    /// </summary>
    public int MaxTokens { get; set; } = 512;

    /// <summary>
    /// Validate post-processing configuration.
    /// </summary>
    /// <exception cref="InvalidOperationException">If configuration is invalid</exception>
    public void Validate()
    {
        if (!Enabled)
        {
            // If disabled, don't validate paths
            return;
        }

        if (string.IsNullOrWhiteSpace(LlmCliPath))
        {
            throw new InvalidOperationException("LLM CLI path must be specified when post-processing is enabled.");
        }

        if (string.IsNullOrWhiteSpace(ModelPath))
        {
            throw new InvalidOperationException("LLM model path must be specified when post-processing is enabled.");
        }

        if (UseGlossary && string.IsNullOrWhiteSpace(GlossaryPath))
        {
            throw new InvalidOperationException("Glossary path must be specified when glossary is enabled.");
        }

        if (Temperature < 0.0f || Temperature > 2.0f)
        {
            throw new InvalidOperationException("Temperature must be between 0.0 and 2.0.");
        }

        if (TopP < 0.0f || TopP > 1.0f)
        {
            throw new InvalidOperationException("TopP must be between 0.0 and 1.0.");
        }

        if (RepeatPenalty < 1.0f || RepeatPenalty > 2.0f)
        {
            throw new InvalidOperationException("RepeatPenalty must be between 1.0 and 2.0.");
        }

        if (MaxTokens <= 0)
        {
            throw new InvalidOperationException("MaxTokens must be greater than 0.");
        }
    }
}
