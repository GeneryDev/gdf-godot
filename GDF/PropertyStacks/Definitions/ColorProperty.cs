using Godot;

namespace GDF.PropertyStacks.Definitions;

[GlobalClass]
[Tool]
public partial class ColorProperty : StandardPropertyDefinition<Color>
{
    [Export] public Color DefaultValue;

    public override Color GetDefaultValue()
    {
        return DefaultValue;
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