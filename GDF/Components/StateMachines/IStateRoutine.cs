namespace GDF.Components.StateMachines;

public interface IStateRoutine
{
    public virtual bool RemoveFromTreeOnDisabled => true;
    public virtual void OnDisabledByTransition(in StateMachine.StateTransitionInfo transition) {}
    public virtual void OnEnabledByTransition(in StateMachine.StateTransitionInfo transition) {}
}