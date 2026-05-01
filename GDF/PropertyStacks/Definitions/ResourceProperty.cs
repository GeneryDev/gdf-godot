using System;
using GDF.PropertyStacks.Internal;
using Godot;

namespace GDF.PropertyStacks.Definitions;

[GlobalClass]
[Tool]
public partial class ResourceProperty : PropertyDefinitionResource,
    IPropertyDefinition<Resource, Resource, Empty>,
    IPropertyAcceptsInput<Resource, Resource>,
    IPropertyAcceptsInput<Variant, Resource>
{
    [Export] public Resource DefaultValue;

    public Resource GetInitialValue(Empty cache)
    {
        return DefaultValue;
    }

    public Resource InputToIntermediate(Resource input)
    {
        return input;
    }

    public Resource InputToIntermediate(Variant input)
    {
        return input.As<Resource>();
    }

    public Resource Reduce(Resource lower, Resource higher, float weight,
        PropertyFrameHandle handle)
    {
        return weight < 1 ? lower : higher;
    }

    public Resource IntermediateToOutput(Resource value)
    {
        return value;
    }

    public Variant OutputToVariant(Resource value)
    {
        return Variant.From(value);
    }

    public override IProperty CreateProperty()
    {
        return new PropertyImpl<Resource, Resource, Empty>(this);
    }

    public Empty CreateCache()
    {
        return new Empty();
    }
}