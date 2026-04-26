using Godot;

namespace GDF.PropertyStacks.Definitions;

[GlobalClass]
[Tool]
public partial class IntProperty : StandardPropertyDefinition<int>
{
    [Export] public int DefaultValue;

    public override int GetDefaultValue()
    {
        return DefaultValue;
    }
    
    public override VectorModification<int> InputToIntermediate(object input)
    {
        if (input is int i) return new VectorModification<int>() { Value = i, Operation = DefaultOperator};
        if (input is float f) return new VectorModification<int>() { Value = (int)f, Operation = DefaultOperator };
        if (input is double d) return new VectorModification<int>() { Value = (int)d, Operation = DefaultOperator };
        return base.InputToIntermediate(input);
    }

    public override int ApplyAdd(int a, int b)
    {
        return a + b;
    }
    public override int ApplyMultiply(int a, int b)
    {
        return a * b;
    }

    public override int Lerp(int a, int b, float weight)
    {
        return (int)Mathf.Lerp(a, b, weight);
    }

    public override Variant OutputToVariant(int value)
    {
        return Variant.From(value);
    }
}