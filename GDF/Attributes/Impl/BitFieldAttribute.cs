namespace GDF.Attributes.Impl;

public class BitFieldAttribute: Attribute<uint, (uint Value, BitFieldAttribute.ModificationOperator Operator, uint Mask)>
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

    protected override (uint Value, ModificationOperator Operator, uint Mask) GetInitialParam()
    {
        return (BaseValue, ModificationOperator.Set, 0);
    }
    
    protected override Modification Reduce(in Modification current, in Modification incoming, float weight)
    {
        if (weight <= 0) return current;

        uint mergedValue;
        switch (incoming.Param.Operator)
        {
            case ModificationOperator.Set:
                mergedValue = (current.Param.Value & ~incoming.Param.Mask) | (incoming.Param.Value & incoming.Param.Mask);
                break;
            case ModificationOperator.And:
                mergedValue = (current.Param.Value & ~incoming.Param.Mask) | ((current.Param.Value & incoming.Param.Value) & incoming.Param.Mask);
                break;
            case ModificationOperator.Or:
                mergedValue = (current.Param.Value & ~incoming.Param.Mask) | ((current.Param.Value | incoming.Param.Value) & incoming.Param.Mask);
                break;
            default:
                GdfLogger.Error($"Operation '{incoming.Param.Operator}' not supported for attribute of type {GetType()}");
                mergedValue = current.Param.Value;
                break;
        }

        return new Modification()
        {
            Param = (mergedValue, ModificationOperator.Set, current.Param.Mask | incoming.Param.Mask),
            Token = incoming.Token
        };
    }

    public enum ModificationOperator
    {
        Set,
        And,
        Or
    }
}