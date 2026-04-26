using System;
using GDF.PropertyStacks.Internal;
using Godot;

namespace GDF.PropertyStacks.Definitions;

[GlobalClass]
[Tool]
public partial class StringProperty : PropertyDefinitionResource, IPropertyDefinition<string, string, Empty>
{
    [Export] public string DefaultValue;

    public string GetInitialValue(Empty cache)
    {
        return DefaultValue;
    }

    public string InputToIntermediate(object input)
    {
        return input switch
        {
            string str => str,
            Variant v => v.AsString(),
            _ => throw new ArgumentOutOfRangeException(nameof(input), input,
                $"Provided type {input?.GetType().Name} is not supported in string property")
        };
    }

    public string Reduce(string lower, string higher, float weight,
        PropertyFrameHandle handle)
    {
        return weight < 1 ? lower : higher;
    }

    public string IntermediateToOutput(string value)
    {
        return value;
    }

    public Variant OutputToVariant(string value)
    {
        return Variant.From(value);
    }

    public override IProperty CreateProperty()
    {
        return new PropertyImpl<string, string, Empty>(this);
    }

    public Empty CreateCache()
    {
        return new Empty();
    }
}