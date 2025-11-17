using FluentAssertions;
using LocalWhisper.Core;
using LocalWhisper.Models;
using Xunit;

namespace LocalWhisper.Tests.Unit;

/// <summary>
/// Unit tests for StateMachine class.
/// </summary>
/// <remarks>
/// Tests cover US-001: Hotkey Toggles State
/// - Valid transitions: Idle→Recording, Recording→Processing, Processing→Idle
/// - Invalid transitions should throw InvalidStateTransitionException
/// - State changes should fire StateChanged event
/// - State transitions should be logged
///
/// See: docs/specification/user-stories-gherkin.md (US-001, lines 34-75)
/// See: docs/iterations/iteration-01-hotkey-skeleton.md
/// </remarks>
public class StateMachineTests
{
    [Fact]
    public void Constructor_SetsInitialStateToIdle()
    {
        // Arrange & Act
        var stateMachine = new StateMachine();

        // Assert
        stateMachine.State.Should().Be(AppState.Idle);
    }

    [Fact]
    public void TransitionTo_IdleToRecording_UpdatesState()
    {
        // Arrange
        var stateMachine = new StateMachine();

        // Act
        stateMachine.TransitionTo(AppState.Recording);

        // Assert
        stateMachine.State.Should().Be(AppState.Recording);
    }

    [Fact]
    public void TransitionTo_RecordingToProcessing_UpdatesState()
    {
        // Arrange
        var stateMachine = new StateMachine();
        stateMachine.TransitionTo(AppState.Recording);

        // Act
        stateMachine.TransitionTo(AppState.Processing);

        // Assert
        stateMachine.State.Should().Be(AppState.Processing);
    }

    [Fact]
    public void TransitionTo_ProcessingToIdle_UpdatesState()
    {
        // Arrange
        var stateMachine = new StateMachine();
        stateMachine.TransitionTo(AppState.Recording);
        stateMachine.TransitionTo(AppState.Processing);

        // Act
        stateMachine.TransitionTo(AppState.Idle);

        // Assert
        stateMachine.State.Should().Be(AppState.Idle);
    }

    [Fact]
    public void TransitionTo_IdleToProcessing_ThrowsInvalidStateTransitionException()
    {
        // Arrange
        var stateMachine = new StateMachine();

        // Act
        Action act = () => stateMachine.TransitionTo(AppState.Processing);

        // Assert
        act.Should().Throw<InvalidStateTransitionException>()
            .WithMessage("Invalid state transition: Idle -> Processing")
            .And.FromState.Should().Be(AppState.Idle);
    }

    [Fact]
    public void TransitionTo_RecordingToIdle_ThrowsInvalidStateTransitionException()
    {
        // Arrange
        var stateMachine = new StateMachine();
        stateMachine.TransitionTo(AppState.Recording);

        // Act
        Action act = () => stateMachine.TransitionTo(AppState.Idle);

        // Assert
        act.Should().Throw<InvalidStateTransitionException>()
            .WithMessage("Invalid state transition: Recording -> Idle");
    }

    [Fact]
    public void TransitionTo_ProcessingToRecording_ThrowsInvalidStateTransitionException()
    {
        // Arrange
        var stateMachine = new StateMachine();
        stateMachine.TransitionTo(AppState.Recording);
        stateMachine.TransitionTo(AppState.Processing);

        // Act
        Action act = () => stateMachine.TransitionTo(AppState.Recording);

        // Assert
        act.Should().Throw<InvalidStateTransitionException>()
            .WithMessage("Invalid state transition: Processing -> Recording");
    }

    [Fact]
    public void TransitionTo_SameState_DoesNothing()
    {
        // Arrange
        var stateMachine = new StateMachine();
        var eventFired = false;
        stateMachine.StateChanged += (s, e) => eventFired = true;

        // Act
        stateMachine.TransitionTo(AppState.Idle);

        // Assert
        stateMachine.State.Should().Be(AppState.Idle);
        eventFired.Should().BeFalse("transitioning to the same state should not fire event");
    }

    [Fact]
    public void TransitionTo_ValidTransition_FiresStateChangedEvent()
    {
        // Arrange
        var stateMachine = new StateMachine();
        StateChangedEventArgs? eventArgs = null;
        stateMachine.StateChanged += (s, e) => eventArgs = e;

        // Act
        stateMachine.TransitionTo(AppState.Recording);

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.OldState.Should().Be(AppState.Idle);
        eventArgs.NewState.Should().Be(AppState.Recording);
        eventArgs.TransitionTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void TransitionTo_MultipleTransitions_AllEventsAreFired()
    {
        // Arrange
        var stateMachine = new StateMachine();
        var eventLog = new List<(AppState From, AppState To)>();
        stateMachine.StateChanged += (s, e) => eventLog.Add((e.OldState, e.NewState));

        // Act
        stateMachine.TransitionTo(AppState.Recording);
        stateMachine.TransitionTo(AppState.Processing);
        stateMachine.TransitionTo(AppState.Idle);

        // Assert
        eventLog.Should().HaveCount(3);
        eventLog[0].Should().Be((AppState.Idle, AppState.Recording));
        eventLog[1].Should().Be((AppState.Recording, AppState.Processing));
        eventLog[2].Should().Be((AppState.Processing, AppState.Idle));
    }

    [Fact]
    public void TransitionTo_InvalidTransition_StateRemainsUnchanged()
    {
        // Arrange
        var stateMachine = new StateMachine();

        // Act
        try
        {
            stateMachine.TransitionTo(AppState.Processing);
        }
        catch (InvalidStateTransitionException)
        {
            // Expected exception
        }

        // Assert
        stateMachine.State.Should().Be(AppState.Idle, "state should not change on invalid transition");
    }
}
