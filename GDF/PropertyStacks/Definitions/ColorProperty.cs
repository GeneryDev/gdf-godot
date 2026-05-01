using GDF.PropertyStacks.Internal;
using Godot;

namespace GDF.PropertyStacks.Definitions;

[GlobalClass]
[Tool]
public partial class ColorProperty : StandardPropertyDefinition<Color>,
    IPropertyAcceptsInput<Color, VectorModification<Color>>,
    IPropertyAcceptsInput<VectorModification<Color>, VectorModification<Color>>
{
    [Export] public Color DefaultValue;

    public override Color GetDefaultValue()
    {
        return DefaultValue;
    }

    public VectorModification<Color> InputToIntermediate(Color input)
    {
        return new VectorModification<Color>() { Value = input, Operation = DefaultOperator };
    }
    public VectorModification<Color> InputToIntermediate(VectorModification<Color> input)
    {
        return input;
    }
    
    public override Color ApplyAdd(Color a, Color b)
    {
        return b.Blend(a);
    }
    public override Color ApplyMultiply(Color a, Color b)
    {
        return a * b;
    }

    public override Color Lerp(Color a, Color b, float weight)
    {
        return a.Lerp(b, weight);
    }

    public override Variant OutputToVariant(Color value)
    {
        return Variant.From(value);
    }
}