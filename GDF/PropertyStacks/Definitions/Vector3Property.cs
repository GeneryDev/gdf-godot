using Godot;

namespace GDF.PropertyStacks.Definitions;

[GlobalClass]
[Tool]
public partial class Vector3Property : StandardPropertyDefinition<Vector3>
{
    [Export] public Vector3 DefaultValue;
    [Export] public bool Slerp;

    public override Vector3 GetDefaultValue()
    {
        return DefaultValue;
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