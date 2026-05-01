using GDF.PropertyStacks.Internal;
using Godot;

namespace GDF.PropertyStacks.Definitions;

[GlobalClass]
[Tool]
public partial class BoolProperty : StandardPropertyDefinition<bool>,
    IPropertyAcceptsInput<bool, VectorModification<bool>>,
    IPropertyAcceptsInput<VectorModification<bool>, VectorModification<bool>>
{
    [Export] public bool DefaultValue;

    public override bool GetDefaultValue()
    {
        return DefaultValue;
    }

    public VectorModification<bool> InputToIntermediate(bool input)
    {
        return new VectorModification<bool>() { Value = input, Operation = DefaultOperator };
    }
    public VectorModification<bool> InputToIntermediate(VectorModification<bool> input)
    {
        return input;
    }
    
    public override bool ApplyAnd(bool a, bool b)
    {
        return a && b;
    }
    public override bool ApplyOr(bool a, bool b)
    {
        return a || b;
    }

    public override bool Lerp(bool a, bool b, float weight)
    {
        return weight < 1 ? a : b;
    }

    public override Variant OutputToVariant(bool value)
    {
        return Variant.From(value);
    }
}