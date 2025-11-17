namespace LocalWhisper.Models;

/// <summary>
/// Exception thrown when an invalid state transition is attempted.
/// </summary>
public class InvalidStateTransitionException : InvalidOperationException
{
    public AppState FromState { get; }
    public AppState ToState { get; }

    public InvalidStateTransitionException(AppState fromState, AppState toState)
        : base($"Invalid state transition: {fromState} -> {toState}")
    {
        FromState = fromState;
        ToState = toState;
    }

    public InvalidStateTransitionException(AppState fromState, AppState toState, string message)
        : base(message)
    {
        FromState = fromState;
        ToState = toState;
    }

    public InvalidStateTransitionException(AppState fromState, AppState toState, string message, Exception innerException)
        : base(message, innerException)
    {
        FromState = fromState;
        ToState = toState;
    }
}
