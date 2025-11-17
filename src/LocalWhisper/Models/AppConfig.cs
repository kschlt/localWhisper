namespace LocalWhisper.Models;

/// <summary>
/// Application configuration model (TOML schema).
/// </summary>
/// <remarks>
/// Iteration 1: Minimal schema (hotkey only).
/// Iteration 5: Full schema (paths, stt, history, postprocessing, logging).
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

    // Future iterations will add:
    // public AppMetadata App { get; set; } = new();
    // public PathsConfig Paths { get; set; } = new();
    // public SttConfig Stt { get; set; } = new();
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
