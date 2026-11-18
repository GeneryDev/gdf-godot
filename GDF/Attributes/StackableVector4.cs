using System;
using Godot;

namespace GDF.Attributes;

public class StackableVector4: StandardStackableValue<Vector4>
{
    public StackableVector4() {}
    public StackableVector4(Vector4 baseValue) : base(baseValue) {}

    public override Vector4 ApplyOperation(Vector4 a, Vector4 b, ModificationOperator op)
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

    protected override Vector4 Lerp(Vector4 a, Vector4 b, float weight)
    {
        return a.Lerp(b, weight);
    }
}