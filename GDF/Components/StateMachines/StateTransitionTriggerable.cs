using Godot;

namespace GDF.Components.StateMachines;

[GlobalClass]
public partial class StateTransitionTriggerable : StateTransition
{
    [Export] public State TargetState;

    private long _triggeredTick = -1;

    public void Trigger()
    {
        _triggeredTick = TargetState?.StateMachine?.TotalTicks ?? -1;
    }
    
    public override bool ProcessTransitions(StateMachine stateMachine)
    {
        if (_triggeredTick > -1 && stateMachine.TotalTicks <= _triggeredTick + 1)
        {
            return stateMachine.TransitionToState(TargetState);
        }
        else
        {
            _triggeredTick = -1;
            return false;
        }
    }
}