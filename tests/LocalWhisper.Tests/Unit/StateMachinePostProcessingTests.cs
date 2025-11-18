using FluentAssertions;
using LocalWhisper.Core;
using LocalWhisper.Models;
using Xunit;

namespace LocalWhisper.Tests.Unit;

/// <summary>
/// Unit tests for StateMachine with PostProcessing state.
/// Tests for US-060 (state machine integration).
/// </summary>
public class StateMachinePostProcessingTests
{
    [Fact]
    public void StateMachine_CanTransitionToPostProcessing()
    {
        // Arrange
        var stateMachine = new StateMachine();
        stateMachine.TransitionTo(AppState.Recording);
        stateMachine.TransitionTo(AppState.Processing);

        // Act
        stateMachine.TransitionTo(AppState.PostProcessing);

        // Assert
        stateMachine.CurrentState.Should().Be(AppState.PostProcessing);
    }

    [Fact]
    public void StateMachine_PostProcessingToIdle_IsValid()
    {
        // Arrange
        var stateMachine = new StateMachine();
        stateMachine.TransitionTo(AppState.Recording);
        stateMachine.TransitionTo(AppState.Processing);
        stateMachine.TransitionTo(AppState.PostProcessing);

        // Act
        stateMachine.TransitionTo(AppState.Idle);

        // Assert
        stateMachine.CurrentState.Should().Be(AppState.Idle);
    }

    [Fact]
    public void StateMachine_ProcessingToIdleDirectly_StillValidForBackwardCompatibility()
    {
        // When post-processing is disabled, we skip PostProcessing state
        // Arrange
        var stateMachine = new StateMachine();
        stateMachine.TransitionTo(AppState.Recording);
        stateMachine.TransitionTo(AppState.Processing);

        // Act
        stateMachine.TransitionTo(AppState.Idle);

        // Assert
        stateMachine.CurrentState.Should().Be(AppState.Idle);
    }

    [Fact]
    public void StateMachine_IdleToPostProcessing_ThrowsException()
    {
        // Arrange
        var stateMachine = new StateMachine();

        // Act
        Action act = () => stateMachine.TransitionTo(AppState.PostProcessing);

        // Assert
        act.Should().Throw<InvalidStateTransitionException>(
            "cannot jump directly to PostProcessing from Idle");
    }

    [Fact]
    public void StateMachine_RecordingToPostProcessing_ThrowsException()
    {
        // Arrange
        var stateMachine = new StateMachine();
        stateMachine.TransitionTo(AppState.Recording);

        // Act
        Action act = () => stateMachine.TransitionTo(AppState.PostProcessing);

        // Assert
        act.Should().Throw<InvalidStateTransitionException>(
            "must go through Processing before PostProcessing");
    }

    [Fact]
    public void StateMachine_PostProcessingStateChange_RaisesEvent()
    {
        // Arrange
        var stateMachine = new StateMachine();
        stateMachine.TransitionTo(AppState.Recording);
        stateMachine.TransitionTo(AppState.Processing);

        AppState? newState = null;
        AppState? oldState = null;
        stateMachine.StateChanged += (sender, args) =>
        {
            oldState = args.OldState;
            newState = args.NewState;
        };

        // Act
        stateMachine.TransitionTo(AppState.PostProcessing);

        // Assert
        oldState.Should().Be(AppState.Processing);
        newState.Should().Be(AppState.PostProcessing);
    }

    [Fact]
    public void AppState_PostProcessingEnumValue_Exists()
    {
        // Arrange & Act
        var postProcessingState = AppState.PostProcessing;

        // Assert
        postProcessingState.Should().BeDefined("PostProcessing state must exist in enum");
        ((int)postProcessingState).Should().Be(3, "PostProcessing should be the 4th state (0-indexed: 3)");
    }
}
