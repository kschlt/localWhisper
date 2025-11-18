namespace LocalWhisper.Models;

/// <summary>
/// Application configuration model (TOML schema).
/// </summary>
/// <remarks>
/// Iteration 1: Minimal schema (hotkey only).
/// Iteration 3: Added Whisper STT configuration.
/// Iteration 5: Full schema (paths, history, postprocessing, logging).
///
/// TODO(PH-003, Iter-5): Expand to full schema
/// See: docs/meta/placeholders-tracker.md (PH-003)
/// See: docs/specification/data-structures.md (lines 49-110)
/// </remarks>
public class AppConfig
{
    /// <summary>
    /// Hotkey configuration.
    /// </summary>
    public HotkeyConfig Hotkey { get; set; } = new();

    /// <summary>
    /// Whisper STT configuration (Iteration 3).
    /// </summary>
    public WhisperConfig Whisper { get; set; } = new();

    // Future iterations will add:
    // public AppMetadata App { get; set; } = new();
    // public PathsConfig Paths { get; set; } = new();
    // public HistoryConfig History { get; set; } = new();
    // public PostProcessingConfig PostProcessing { get; set; } = new();
    // public LoggingConfig Logging { get; set; } = new();
}

/// <summary>
/// Hotkey configuration section.
/// </summary>
public class HotkeyConfig
{
    /// <summary>
    /// Modifier keys (Ctrl, Shift, Alt, Win).
    /// Must contain at least one modifier.
    /// </summary>
    public List<string> Modifiers { get; set; } = new() { "Ctrl", "Shift" };

    /// <summary>
    /// Main key (A-Z, 0-9, F1-F12, etc.).
    /// </summary>
    public string Key { get; set; } = "D";

    /// <summary>
    /// Validate hotkey configuration.
    /// </summary>
    /// <exception cref="InvalidOperationException">If configuration is invalid</exception>
    public void Validate()
    {
        if (Modifiers == null || Modifiers.Count == 0)
        {
            throw new InvalidOperationException("Hotkey must have at least one modifier key.");
        }

        if (string.IsNullOrWhiteSpace(Key))
        {
            throw new InvalidOperationException("Hotkey must have a main key.");
        }

        // Validate modifier keys
        var validModifiers = new HashSet<string> { "Ctrl", "Shift", "Alt", "Win" };
        foreach (var modifier in Modifiers)
        {
            if (!validModifiers.Contains(modifier))
            {
                throw new InvalidOperationException($"Invalid modifier key: {modifier}. Valid: Ctrl, Shift, Alt, Win.");
            }
        }
    }
}

/// <summary>
/// Whisper STT configuration section (Iteration 3).
/// </summary>
public class WhisperConfig
{
    /// <summary>
    /// Path to Whisper CLI executable.
    /// Default: "whisper-cli" (assumes in PATH).
    /// </summary>
    public string CLIPath { get; set; } = "whisper-cli";

    /// <summary>
    /// Path to Whisper model file (e.g., ggml-small.bin).
    /// Default: Empty (must be configured before first use).
    /// </summary>
    public string ModelPath { get; set; } = string.Empty;

    /// <summary>
    /// Language code for transcription (e.g., "de", "en").
    /// Default: "de" (German).
    /// </summary>
    public string Language { get; set; } = "de";

    /// <summary>
    /// Timeout for STT processing in seconds.
    /// Default: 60 seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Validate Whisper configuration.
    /// </summary>
    /// <exception cref="InvalidOperationException">If configuration is invalid</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(CLIPath))
        {
            throw new InvalidOperationException("Whisper CLI path must be specified.");
        }

        if (string.IsNullOrWhiteSpace(ModelPath))
        {
            throw new InvalidOperationException("Whisper model path must be specified.");
        }

        if (TimeoutSeconds <= 0)
        {
            throw new InvalidOperationException("Whisper timeout must be greater than 0 seconds.");
        }
    }
}
