using System.Collections.Generic;
using GDF.PropertyStacks.Definitions.Specialized;
using GDF.PropertyStacks.Internal;
using Godot;

namespace GDF.PropertyStacks;

[Tool]
[GlobalClass]
public partial class PropertyRegistryInputGroups : PropertyRegistryDynamic
{
    public override void PopulatePropertiesDictionary(Dictionary<string, IProperty> properties)
    {
        foreach (string id in InputGroups.GetAll())
        {
            properties[id] = new InputGroupProperty() { PropertyId = id }.CreateProperty();
        }
    }
}