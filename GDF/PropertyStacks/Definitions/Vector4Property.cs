using GDF.PropertyStacks.Internal;
using Godot;

namespace GDF.PropertyStacks.Definitions;

[GlobalClass]
[Tool]
public partial class Vector4Property : StandardPropertyDefinition<Vector4>,
    IPropertyAcceptsInput<Vector4, VectorModification<Vector4>>,
    IPropertyAcceptsInput<VectorModification<Vector4>, VectorModification<Vector4>>
{
    [Export] public Vector4 DefaultValue;

    public override Vector4 GetDefaultValue()
    {
        return DefaultValue;
    }

    public VectorModification<Vector4> InputToIntermediate(Vector4 input)
    {
        return new VectorModification<Vector4>() { Value = input, Operation = DefaultOperator };
    }
    public VectorModification<Vector4> InputToIntermediate(VectorModification<Vector4> input)
    {
        return input;
    }
    
    public override Vector4 ApplyAdd(Vector4 a, Vector4 b)
    {
        return a + b;
    }
    public override Vector4 ApplyMultiply(Vector4 a, Vector4 b)
    {
        return a * b;
    }

    public override Vector4 Lerp(Vector4 a, Vector4 b, float weight)
    {
        return a.Lerp(b, weight);
    }

    public override Variant OutputToVariant(Vector4 value)
    {
        return Variant.From(value);
    }
}