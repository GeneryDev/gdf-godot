using Godot;

namespace GDF.PropertyStacks.Definitions;

[GlobalClass]
[Tool]
public partial class Vector4Property : StandardPropertyDefinition<Vector4>
{
    [Export] public Vector4 DefaultValue;

    public override Vector4 GetDefaultValue()
    {
        return DefaultValue;
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