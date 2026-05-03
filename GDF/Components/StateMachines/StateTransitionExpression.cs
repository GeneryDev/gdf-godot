using GDF.Util;
using Godot;

namespace GDF.Components.StateMachines;

[GlobalClass]
public partial class StateTransitionExpression : StateTransition
{
    [Export] public State TargetState;

    [Export(PropertyHint.Expression)]
    public string Condition
    {
        get => _condition;
        set
        {
            if (_condition == value) return;
            _condition = value;
            _expressionNeedsRecompiling = true;
        }
    }
    
    [Export] public Node ExpressionBaseNode;
    
    [Export] public float MinStateTime = 0;
    
    private string _condition;
    
    private Expression _expression;
    private bool _expressionNeedsRecompiling = false;
    
    public override bool ProcessTransitions(StateMachine stateMachine)
    {
        if (stateMachine.StateTimeSec < MinStateTime) return false;
        bool result;
        EnsureExpressionCompiled();
        if (_expression != null)
        {
            result = _expression.Execute(null, ExpressionBaseNode).AsBool();
            if (_expression.HasExecuteFailed())
            {
                GD.PushWarning(
                    $"Failed to execute transition condition expression, at {GetPath()}\nExpression: {Condition}");
                return false;
            }
        }
        else if (string.IsNullOrEmpty(Condition))
        {
            // expression is empty, assume true
            result = true;
        }
        else
        {
            // parse error in expression, do not transition
            return false;
        }

        if (!result) return false;

        return stateMachine.TransitionToState(TargetState);
    }

    private void EnsureExpressionCompiled()
    {
        if (!_expressionNeedsRecompiling) return;
        _expression = ExpressionUtil.Parse(Condition);
        _expressionNeedsRecompiling = false;
    }
}