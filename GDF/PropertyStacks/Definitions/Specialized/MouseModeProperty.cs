using System;
using GDF.PropertyStacks.Internal;
using Godot;

namespace GDF.PropertyStacks.Definitions.Specialized;

using Input = Godot.Input;

[GlobalClass]
[Tool]
public partial class MouseModeProperty : PropertyDefinitionResource, IPropertyDefinition<Input.MouseModeEnum, Input.MouseModeEnum, Empty>
{
    [Export] public Input.MouseModeEnum DefaultValue;

    public Input.MouseModeEnum GetInitialValue(Empty cache)
    {
        return DefaultValue;
    }

    public Input.MouseModeEnum InputToIntermediate(object input)
    {
        return input switch
        {
            Input.MouseModeEnum mode => mode,
            Variant v => v.As<Input.MouseModeEnum>(),
            _ => throw new ArgumentOutOfRangeException(nameof(input), input,
                $"Provided type {input?.GetType().Name} is not supported in mouse mode property")
        };
    }

    public Input.MouseModeEnum Reduce(Input.MouseModeEnum lower, Input.MouseModeEnum higher, float weight,
        PropertyFrameHandle handle)
    {
        return weight < 1 ? lower : higher;
    }

    public Input.MouseModeEnum IntermediateToOutput(Input.MouseModeEnum value)
    {
        return value;
    }

    public Variant OutputToVariant(Input.MouseModeEnum value)
    {
        return Variant.From(value);
    }

    public override IProperty CreateProperty()
    {
        return new PropertyImpl<Input.MouseModeEnum, Input.MouseModeEnum, Empty>(this);
    }

    public Empty CreateCache()
    {
        return new Empty();
    }
}