using System;
using Godot;

namespace GDF.Attributes;

public class StackableInt: StandardStackableValue<int>
{
    public StackableInt() {}
    public StackableInt(int baseValue) : base(baseValue) {}

    public override int ApplyOperation(int a, int b, ModificationOperator op)
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
                return a & b;
            case ModificationOperator.Or:
                return a | b;
            default:
                throw new NotSupportedException();
        }
    }

    protected override int Lerp(int a, int b, float weight)
    {
        return (int)Mathf.Lerp(a, b, weight);
    }
}