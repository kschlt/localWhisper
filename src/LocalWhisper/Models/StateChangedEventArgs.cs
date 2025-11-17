namespace LocalWhisper.Models;

/// <summary>
/// Event arguments for state transition events.
/// </summary>
public class StateChangedEventArgs : EventArgs
{
    public AppState OldState { get; }
    public AppState NewState { get; }
    public DateTime TransitionTime { get; }

    public StateChangedEventArgs(AppState oldState, AppState newState)
    {
        OldState = oldState;
        NewState = newState;
        TransitionTime = DateTime.UtcNow;
    }
}
