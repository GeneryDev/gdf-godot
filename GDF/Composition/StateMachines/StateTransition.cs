using Godot;

namespace GDF.Composition.StateMachines;

[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/state_transition.png")]
public abstract partial class StateTransition : Node
{
    [Signal]
    public delegate void TransitionTriggeredEventHandler();
    
    public abstract bool ProcessTransitions(StateMachine stateMachine);
}