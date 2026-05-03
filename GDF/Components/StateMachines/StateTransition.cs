using Godot;

namespace GDF.Components.StateMachines;

[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/state_transition.png")]
public abstract partial class StateTransition : Node
{
    public abstract bool ProcessTransitions(StateMachine stateMachine);
}