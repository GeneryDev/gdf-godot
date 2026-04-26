using System;
using GDF.PropertyStacks.Internal;
using Godot;

namespace GDF.PropertyStacks.Definitions;

[GlobalClass]
[Tool]
public partial class ResourceProperty : PropertyDefinitionResource, IPropertyDefinition<Resource, Resource, Empty>
{
    [Export] public Resource DefaultValue;

    public Resource GetInitialValue(Empty cache)
    {
        return DefaultValue;
    }

    public Resource InputToIntermediate(object input)
    {
        return input switch
        {
            Resource res => res,
            Variant v => v.As<Resource>(),
            _ => throw new ArgumentOutOfRangeException(nameof(input), input,
                $"Provided type {input?.GetType().Name} is not supported in resource property")
        };
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