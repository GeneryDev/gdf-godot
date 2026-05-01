using System;
using GDF.PropertyStacks.Internal;
using Godot;
using Godot.Collections;

namespace GDF.PropertyStacks.Definitions;

[GlobalClass]
[Tool]
public partial class BitFieldProperty : PropertyDefinitionResource,
    IPropertyDefinition<BitFieldModification, uint, Empty>,
    IPropertyAcceptsInput<BitFieldModification, BitFieldModification>
{
    [Export(PropertyHint.Layers3DRender)] public uint DefaultValue;
    
    public BitFieldModification GetInitialValue(Empty cache)
    {
        return new BitFieldModification()
        {
            Value = DefaultValue,
            Mask = ~(uint)0
        };
    }

    public BitFieldModification InputToIntermediate(BitFieldModification input)
    {
        return input;
    }

    public BitFieldModification Reduce(BitFieldModification lower, BitFieldModification higher, float weight,
        PropertyFrameHandle handle)
    {
        if (weight <= 0) return lower;
        uint mergedValue;
        switch (higher.Operation)
        {
            case ModificationOperation.Set:
                mergedValue = (lower.Value & ~higher.Mask) | (higher.Value & higher.Mask);
                break;
            case ModificationOperation.And:
                mergedValue = (lower.Value & ~higher.Mask) | ((lower.Value & higher.Value) & higher.Mask);
                break;
            case ModificationOperation.Or:
                mergedValue = (lower.Value & ~higher.Mask) | ((lower.Value | higher.Value) & higher.Mask);
                break;
            case ModificationOperation.Add:
            case ModificationOperation.Multiply:
            default:
                throw new NotSupportedException();
        }

        return new BitFieldModification()
        {
            Operation = ModificationOperation.Set,
            Value = mergedValue,
            Mask = lower.Mask | higher.Mask
        };
    }

    public uint IntermediateToOutput(BitFieldModification value)
    {
        return value.Value & value.Mask;
    }

    public Variant OutputToVariant(uint value)
    {
        return value;
    }

    public override IProperty CreateProperty()
    {
        return new PropertyImpl<BitFieldModification, uint, Empty>(this);
    }

    public Variant IntermediateToDebug(BitFieldModification value)
    {
        return new Dictionary()
        {
            { "value", value.Value.ToString("B") },
            { "mask", value.Mask.ToString("B") },
            { "op", value.Operation.ToString() }
        };
    }

    public Empty CreateCache()
    {
        return default;
    }
}

public struct BitFieldModification
{
    public ModificationOperation Operation = ModificationOperation.Set;
    public uint Value;
    public uint Mask;

    public BitFieldModification()
    {
        Value = default;
    }

    public static BitFieldModification Set(uint mask)
    {
        return new BitFieldModification()
        {
            Operation = ModificationOperation.Set,
            Value = ~(uint)0,
            Mask = mask
        };
    }

    public static BitFieldModification Unset(uint mask)
    {
        return new BitFieldModification()
        {
            Operation = ModificationOperation.Set,
            Value = 0,
            Mask = mask
        };
    }
}