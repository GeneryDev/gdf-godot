using GDF.PropertyStacks.Internal;
using Godot;
using Godot.Collections;

namespace GDF.PropertyStacks;

[Tool]
public abstract partial class PropertyRegistryDynamic : PropertyRegistry
{
    public abstract override void PopulatePropertiesDictionary(System.Collections.Generic.Dictionary<string, IProperty> properties);

    public override void _ValidateProperty(Dictionary property)
    {
        var propName = property["name"].AsStringName();
        var usage = property["usage"].As<PropertyUsageFlags>();

        if (propName == PropertyRegistry.PropertyName.Definitions || propName == PropertyRegistry.PropertyName.OtherRegistries)
            usage &= ~(PropertyUsageFlags.Editor | PropertyUsageFlags.Storage);
        
        property["usage"] = Variant.From(usage);
        base._ValidateProperty(property);
    }
}