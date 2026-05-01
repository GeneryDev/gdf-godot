using System;
using GDF.PropertyStacks.Internal;
using Godot;

namespace GDF.PropertyStacks.Definitions.Specialized;

using Input = Godot.Input;

[GlobalClass]
[Tool]
public partial class MouseModeProperty : PropertyDefinitionResource,
    IPropertyDefinition<Input.MouseModeEnum, Input.MouseModeEnum, Empty>,
    IPropertyAcceptsInput<Input.MouseModeEnum, Input.MouseModeEnum>,
    IPropertyAcceptsInput<Variant, Input.MouseModeEnum>
{
    [Export] public Input.MouseModeEnum DefaultValue;

    public Input.MouseModeEnum GetInitialValue(Empty cache)
    {
        return DefaultValue;
    }

    public Input.MouseModeEnum InputToIntermediate(Input.MouseModeEnum input)
    {
        return input;
    }

    public Input.MouseModeEnum InputToIntermediate(Variant input)
    {
        return input.As<Input.MouseModeEnum>();
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