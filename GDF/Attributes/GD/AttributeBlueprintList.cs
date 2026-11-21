using Godot;
using Godot.Collections;

namespace GDF.Attributes;

[GlobalClass]
public partial class AttributeBlueprintList : AttributeBlueprint
{
    [Export] public Array<AttributeBlueprint> Blueprints = new();
    
    public override void CreateAttributes(System.Collections.Generic.Dictionary<StringName, IAttribute> output)
    {
        foreach (var blueprint in Blueprints)
        {
            blueprint?.CreateAttributes(output);
        }
    }
}