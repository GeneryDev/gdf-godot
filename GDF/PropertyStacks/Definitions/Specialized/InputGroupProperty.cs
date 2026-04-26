using System;
using GDF.PropertyStacks.Internal;
using Godot;

namespace GDF.PropertyStacks.Definitions.Specialized;

[GlobalClass]
[Tool]
public partial class InputGroupProperty : PropertyDefinitionResource, IPropertyDefinition<InputGroupProperty.FrameData, InputGroupProperty.FrameData, Empty>
{
    public FrameData GetInitialValue(Empty cache)
    {
        return new FrameData() { BlockingHandle = default };
    }

    public FrameData InputToIntermediate(object input)
    {
        return input switch
        {
            InputGroupMode mode => new FrameData() { Mode = mode },
            Variant v => new FrameData() { Mode = v.As<InputGroupMode>() },
            _ => throw new ArgumentOutOfRangeException(nameof(input), input,
                $"Provided type {input?.GetType().Name} is not supported in input group property")
        };
    }

    public FrameData Reduce(FrameData lower, FrameData higher, float weight, PropertyFrameHandle handle)
    {
        if ((higher.Mode & InputGroupMode.BlocksInput) != 0)
        {
            return higher with { BlockingHandle = handle };
        }

        return lower;
    }

    public FrameData IntermediateToOutput(FrameData value)
    {
        return value;
    }

    public Variant OutputToVariant(FrameData value)
    {
        return Variant.From(value.BlockingHandle.Id);
    }

    public override IProperty CreateProperty()
    {
        return new PropertyImpl<FrameData, FrameData, Empty>(this);
    }

    public Empty CreateCache()
    {
        return new Empty();
    }

    public struct FrameData
    {
        public InputGroupMode Mode;
        public PropertyFrameHandle BlockingHandle;
    }
}
[Flags]
public enum InputGroupMode
{
    AcceptsInput = 1,
    BlocksInput = 2,
    
    Disable = BlocksInput,
    Capture = AcceptsInput | BlocksInput,
    PassThrough = AcceptsInput,
}