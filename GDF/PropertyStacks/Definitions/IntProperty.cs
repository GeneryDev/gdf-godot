using GDF.PropertyStacks.Internal;
using Godot;

namespace GDF.PropertyStacks.Definitions;

[GlobalClass]
[Tool]
public partial class IntProperty : StandardPropertyDefinition<int>,
    IPropertyAcceptsInput<int, VectorModification<int>>,
    IPropertyAcceptsInput<float, VectorModification<int>>,
    IPropertyAcceptsInput<double, VectorModification<int>>,
    IPropertyAcceptsInput<VectorModification<int>, VectorModification<int>>
{
    [Export] public int DefaultValue;

    public override int GetDefaultValue()
    {
        return DefaultValue;
    }

    public VectorModification<int> InputToIntermediate(int input)
    {
        return new VectorModification<int>() { Value = input, Operation = DefaultOperator };
    }

    public VectorModification<int> InputToIntermediate(float input)
    {
        return new VectorModification<int>() { Value = (int)input, Operation = DefaultOperator };
    }

    public VectorModification<int> InputToIntermediate(double input)
    {
        return new VectorModification<int>() { Value = (int)input, Operation = DefaultOperator };
    }
    public VectorModification<int> InputToIntermediate(VectorModification<int> input)
    {
        return input;
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