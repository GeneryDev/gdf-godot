using System;
using Godot;

namespace GDF.Attributes;

public class StackableVector3: StandardStackableValue<Vector3>
{
    public StackableVector3() {}
    public StackableVector3(Vector3 baseValue) : base(baseValue) {}

    public override Vector3 ApplyOperation(Vector3 a, Vector3 b, ModificationOperator op)
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

    protected override Vector3 Lerp(Vector3 a, Vector3 b, float weight)
    {
        return a.Lerp(b, weight);
    }
}