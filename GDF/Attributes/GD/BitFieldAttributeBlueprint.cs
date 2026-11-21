using GDF.Attributes.Impl;
using GDF.Util;
using Godot;

namespace GDF.Attributes;

[GlobalClass]
public partial class BitFieldAttributeBlueprint : AttributeBlueprint
{
    [Export]
    public StringName Id = "";

    [Export(PropertyHint.Layers3DRender)] public uint BaseValue;

    public override void CreateAttributes(System.Collections.Generic.Dictionary<StringName, IAttribute> output)
    {
        if (Id.IsNullOrEmpty()) return;
        var attr = CreateAttribute();
        if (attr == null) return;
        output[Id] = attr;
    }

    private IAttribute CreateAttribute()
    {
        return new BitFieldAttribute()
        {
            BaseValue = BaseValue
        };
    }
}