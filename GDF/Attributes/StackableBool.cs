using System;

namespace GDF.Attributes;

public class StackableBool: StandardStackableValue<bool>
{
    public StackableBool() {}
    public StackableBool(bool baseValue) : base(baseValue) {}

    public override bool ApplyOperation(bool a, bool b, ModificationOperator op)
    {
        switch (op)
        {
            case ModificationOperator.Set:
                return b;
            case ModificationOperator.And:
                return a && b;
            case ModificationOperator.Or:
                return a || b;
            case ModificationOperator.Add:
            case ModificationOperator.Multiply:
            default:
                throw new NotSupportedException();
        }
    }

    protected override bool Lerp(bool a, bool b, float weight)
    {
        return weight < 1 ? a : b;
    }
}