namespace LocalWhisper.Models;

/// <summary>
/// Represents the application's current operational state.
/// </summary>
/// <remarks>
/// Valid state transitions:
/// - Idle → Recording (hotkey down)
/// - Recording → Processing (hotkey up)
/// - Processing → PostProcessing (STT complete, if post-processing enabled)
/// - Processing → Idle (STT complete, if post-processing disabled)
/// - PostProcessing → Idle (post-processing complete)
///
/// See: docs/iterations/iteration-01-hotkey-skeleton.md
/// See: docs/iterations/iteration-07-post-processing-DECISIONS.md (Iteration 7)
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
    Processing,

    /// <summary>
    /// Transcript is being post-processed (LLM formatting).
    /// Iteration 7: Added for post-processing feature.
    /// </summary>
    PostProcessing
}
