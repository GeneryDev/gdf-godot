using GDF.PropertyStacks.Internal;
using Godot;

namespace GDF.PropertyStacks.Definitions;

[GlobalClass]
[Tool]
public partial class Vector2Property : StandardPropertyDefinition<Vector2>,
    IPropertyAcceptsInput<Vector2, VectorModification<Vector2>>,
    IPropertyAcceptsInput<VectorModification<Vector2>, VectorModification<Vector2>>
{
    [Export] public Vector2 DefaultValue;
    [Export] public bool Slerp;

    public override Vector2 GetDefaultValue()
    {
        return DefaultValue;
    }

    public VectorModification<Vector2> InputToIntermediate(Vector2 input)
    {
        return new VectorModification<Vector2>() { Value = input, Operation = DefaultOperator };
    }
    public VectorModification<Vector2> InputToIntermediate(VectorModification<Vector2> input)
    {
        return input;
    }
    
    public override Vector2 ApplyAdd(Vector2 a, Vector2 b)
    {
        return a + b;
    }
    public override Vector2 ApplyMultiply(Vector2 a, Vector2 b)
    {
        return a * b;
    }

    public override Vector2 Lerp(Vector2 a, Vector2 b, float weight)
    {
        return Slerp ? ((a - b).IsZeroApprox() ? b : a.Slerp(b, weight)) : a.Lerp(b, weight);
    }

    public override Variant OutputToVariant(Vector2 value)
    {
        return Variant.From(value);
    }
}