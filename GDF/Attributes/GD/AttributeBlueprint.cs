using System.Collections.Generic;
using Godot;

namespace GDF.Attributes;

[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/attribute.svg")]
public abstract partial class AttributeBlueprint : Resource, IAttributeBlueprint
{
    public abstract void CreateAttributes(Dictionary<StringName, IAttribute> output);
}

public interface IAttributeBlueprint
{
    public void CreateAttributes(Dictionary<StringName, IAttribute> output);
}