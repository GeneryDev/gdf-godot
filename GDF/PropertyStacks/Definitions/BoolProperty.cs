using Godot;

namespace GDF.PropertyStacks.Definitions;

[GlobalClass]
[Tool]
public partial class BoolProperty : StandardPropertyDefinition<bool>
{
    [Export] public bool DefaultValue;

    public override bool GetDefaultValue()
    {
        return DefaultValue;
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