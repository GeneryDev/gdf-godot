using GDF.PropertyStacks.Internal;
using Godot;

namespace GDF.PropertyStacks.Definitions;

[GlobalClass]
[Tool]
public partial class Vector3Property : StandardPropertyDefinition<Vector3>,
    IPropertyAcceptsInput<Vector3, VectorModification<Vector3>>,
    IPropertyAcceptsInput<VectorModification<Vector3>, VectorModification<Vector3>>
{
    [Export] public Vector3 DefaultValue;
    [Export] public bool Slerp;

    public override Vector3 GetDefaultValue()
    {
        return DefaultValue;
    }

    public VectorModification<Vector3> InputToIntermediate(Vector3 input)
    {
        return new VectorModification<Vector3>() { Value = input, Operation = DefaultOperator };
    }
    public VectorModification<Vector3> InputToIntermediate(VectorModification<Vector3> input)
    {
        return input;
    }
    
    public override Vector3 ApplyAdd(Vector3 a, Vector3 b)
    {
        return a + b;
    }
    public override Vector3 ApplyMultiply(Vector3 a, Vector3 b)
    {
        return a * b;
    }

    public override Vector3 Lerp(Vector3 a, Vector3 b, float weight)
    {
        return Slerp ? ((a - b).IsZeroApprox() ? b : a.Slerp(b, weight)) : a.Lerp(b, weight);
    }

    public override Variant OutputToVariant(Vector3 value)
    {
        return Variant.From(value);
    }
}