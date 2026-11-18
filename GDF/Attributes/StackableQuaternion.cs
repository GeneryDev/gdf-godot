using System;
using Godot;

namespace GDF.Attributes;

public class StackableQuaternion: StandardStackableValue<Quaternion>
{
    public StackableQuaternion() : base(Quaternion.Identity) {}
    public StackableQuaternion(Quaternion baseValue) : base(baseValue) {}

    public override Quaternion ApplyOperation(Quaternion a, Quaternion b, ModificationOperator op)
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

    protected override Quaternion Lerp(Quaternion a, Quaternion b, float weight)
    {
        return a.Slerp(b, weight);
    }
}