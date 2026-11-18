using System;
using Godot;

namespace GDF.Attributes;

public class StackableVector2: StandardStackableValue<Vector2>
{
    public StackableVector2() {}
    public StackableVector2(Vector2 baseValue) : base(baseValue) {}

    public override Vector2 ApplyOperation(Vector2 a, Vector2 b, ModificationOperator op)
    {
        switch (op)
        {
            case ModificationOperator.Set:
                return b;
            case ModificationOperator.Add:
                return a + b;
            case ModificationOperator.Multiply:
                return a * b;
            case ModificationOperator.And:
            case ModificationOperator.Or:
            default:
                throw new NotSupportedException();
        }
    }

    protected override Vector2 Lerp(Vector2 a, Vector2 b, float weight)
    {
        return a.Lerp(b, weight);
    }
}