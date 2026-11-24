namespace GDF.Attributes.Impl;

public class BitFieldAttribute: Attribute<uint, BitFieldModificationStep, BitFieldModifierChain>
{
    public uint BaseValue
    {
        get => _baseValue;
        set
        {
            if (_baseValue == value) return;
            _baseValue = value;
            MarkDirty();
        }
    }
    public override uint CurrentValue => GetComputed().Param.Value;
    
    private uint _baseValue;

    protected override BitFieldModificationStep GetInitialParam()
    {
        return new (BaseValue, BitFieldModificationStep.ModificationOperator.Set, 0);
    }

    public static implicit operator uint(BitFieldAttribute attribute)
    {
        return attribute.CurrentValue;
    }
    
}
public class BitFieldModifierChain: ModifierChain<BitFieldModificationStep>
{
    protected override void Reduce(ref Step current, in Step incoming, float weight)
    {
        if (weight <= 0) return;

        uint mergedValue;
        switch (incoming.Param.Operator)
        {
            case BitFieldModificationStep.ModificationOperator.Set:
                mergedValue = (current.Param.Value & ~incoming.Param.Mask) | (incoming.Param.Value & incoming.Param.Mask);
                break;
            case BitFieldModificationStep.ModificationOperator.And:
                mergedValue = (current.Param.Value & ~incoming.Param.Mask) | ((current.Param.Value & incoming.Param.Value) & incoming.Param.Mask);
                break;
            case BitFieldModificationStep.ModificationOperator.Or:
                mergedValue = (current.Param.Value & ~incoming.Param.Mask) | ((current.Param.Value | incoming.Param.Value) & incoming.Param.Mask);
                break;
            default:
                GdfLogger.Error($"Operation '{incoming.Param.Operator}' not supported for attribute of type {GetType()}");
                mergedValue = current.Param.Value;
                break;
        }

        current = new Step()
        {
            Param = new (mergedValue, BitFieldModificationStep.ModificationOperator.Set, current.Param.Mask | incoming.Param.Mask),
            Token = incoming.Token
        };
    }
}

public struct BitFieldModificationStep
{
    public uint Value;
    public ModificationOperator Operator;
    public uint Mask;

    public enum ModificationOperator
    {
        Set,
        And,
        Or
    }

    public BitFieldModificationStep(uint value, ModificationOperator @operator, uint mask)
    {
        Value = value;
        Operator = @operator;
        Mask = mask;
    }
}