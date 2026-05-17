namespace GDF.Composition.StateMachines;

public interface IStateRoutineListener
{
    public virtual void OnDisabledByTransition(in StateMachine.StateTransitionInfo transition) {}
    public virtual void OnEnabledByTransition(in StateMachine.StateTransitionInfo transition) {}
}