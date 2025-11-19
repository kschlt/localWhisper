using LocalWhisper.Models;

namespace LocalWhisper.Core;

/// <summary>
/// Manages application state transitions.
/// </summary>
/// <remarks>
/// Valid state transitions:
/// - Idle → Recording (hotkey down)
/// - Recording → Processing (hotkey up)
/// - Processing → PostProcessing (STT complete, if post-processing enabled)
/// - Processing → Idle (STT complete, if post-processing disabled)
/// - PostProcessing → Idle (post-processing complete)
///
/// Invalid transitions throw InvalidStateTransitionException.
/// All state changes are logged and fire StateChanged event.
///
/// See: US-001 (Hotkey Toggles State)
/// See: docs/iterations/iteration-01-hotkey-skeleton.md
/// See: docs/iterations/iteration-07-post-processing-DECISIONS.md (Iteration 7)
/// </remarks>
public class StateMachine
{
    private AppState _currentState;

    /// <summary>
    /// Current application state.
    /// </summary>
    public AppState State => _currentState;

    /// <summary>
    /// Event fired when state transitions occur.
    /// </summary>
    public event EventHandler<StateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Initialize state machine in Idle state.
    /// </summary>
    public StateMachine()
    {
        _currentState = AppState.Idle;
    }

    /// <summary>
    /// Transition to a new state.
    /// </summary>
    /// <param name="newState">Target state</param>
    /// <exception cref="InvalidStateTransitionException">If transition is not valid</exception>
    public void TransitionTo(AppState newState)
    {
        // No-op if transitioning to same state
        if (_currentState == newState)
        {
            return;
        }

        // Validate transition
        if (!IsValidTransition(_currentState, newState))
        {
            throw new InvalidStateTransitionException(_currentState, newState);
        }

        // Perform transition
        var oldState = _currentState;
        _currentState = newState;

        // Log transition
        AppLogger.LogInformation("State transition", new
        {
            From = oldState.ToString(),
            To = newState.ToString()
        });

        // Fire event
        StateChanged?.Invoke(this, new StateChangedEventArgs(oldState, newState));
    }

    /// <summary>
    /// Check if a state transition is valid.
    /// </summary>
    private static bool IsValidTransition(AppState from, AppState to)
    {
        return (from, to) switch
        {
            (AppState.Idle, AppState.Recording) => true,
            (AppState.Recording, AppState.Processing) => true,
            (AppState.Processing, AppState.PostProcessing) => true,  // Iteration 7
            (AppState.Processing, AppState.Idle) => true,
            (AppState.PostProcessing, AppState.Idle) => true,  // Iteration 7
            _ => false
        };
    }
}
