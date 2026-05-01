using GDF.PropertyStacks.Internal;
using Godot;

namespace GDF.PropertyStacks.Definitions;

[GlobalClass]
[Tool]
public partial class QuaternionProperty : StandardPropertyDefinition<Quaternion>,
    IPropertyAcceptsInput<Quaternion, VectorModification<Quaternion>>,
    IPropertyAcceptsInput<VectorModification<Quaternion>, VectorModification<Quaternion>>
{
    [Export] public Quaternion DefaultValue = Quaternion.Identity;

    public override Quaternion GetDefaultValue()
    {
        return DefaultValue;
    }

    public VectorModification<Quaternion> InputToIntermediate(Quaternion input)
    {
        return new VectorModification<Quaternion>() { Value = input, Operation = DefaultOperator };
    }
    public VectorModification<Quaternion> InputToIntermediate(VectorModification<Quaternion> input)
    {
        return input;
    }
    
    public override Quaternion ApplyAdd(Quaternion a, Quaternion b)
    {
        return a + b;
    }
    public override Quaternion ApplyMultiply(Quaternion a, Quaternion b)
    {
        return a * b;
    }

    public override Quaternion Lerp(Quaternion a, Quaternion b, float weight)
    {
        return a.Slerp(b, weight);
    }

    public override Variant OutputToVariant(Quaternion value)
    {
        return Variant.From(value);
    }
}