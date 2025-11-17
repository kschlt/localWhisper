namespace LocalWhisper.Models;

/// <summary>
/// Represents the application's current operational state.
/// </summary>
/// <remarks>
/// Valid state transitions:
/// - Idle → Recording (hotkey down)
/// - Recording → Processing (hotkey up)
/// - Processing → Idle (processing complete)
///
/// See: docs/iterations/iteration-01-hotkey-skeleton.md
/// </remarks>
public enum AppState
{
    /// <summary>
    /// Application is idle, waiting for user input.
    /// </summary>
    Idle,

    /// <summary>
    /// Audio is being recorded (hotkey is held down).
    /// </summary>
    Recording,

    /// <summary>
    /// Audio is being processed (STT transcription).
    /// </summary>
    Processing
}
