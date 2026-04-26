using System.Collections.Generic;
using GDF.PropertyStacks.Internal;
using Godot;
using PropertyDefinitionResource = GDF.PropertyStacks.Internal.PropertyDefinitionResource;

namespace GDF.PropertyStacks;

[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/property_stack_registry.png")]
public partial class PropertyRegistry : Resource
{
    [Export] public PropertyDefinitionResource[] Definitions;
    [Export] public PropertyRegistry[] OtherRegistries;

    public void PopulatePropertiesDictionary(Dictionary<string, IProperty> properties)
    {
        if (Definitions != null)
        {
            foreach (var def in Definitions)
            {
                string id = def.PropertyId;
                properties[id] = def.CreateProperty();
            }
        }

        if (OtherRegistries != null)
        {
            foreach (var registry in OtherRegistries)
            {
                registry.PopulatePropertiesDictionary(properties);
            }
        }
    }
}