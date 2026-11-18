using Godot;

namespace GDF.Attributes;

public class StackableBitField: StackableValue<uint, (uint Value, ModificationOperator Operator, uint Mask)>
{
    public override uint CurrentValue => GetComputed().Param.Value;
    public uint BaseValue;

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
            case ModificationOperator.Add:
            case ModificationOperator.Multiply:
            default:
                GD.PushError($"Operation '{incoming.Param.Operator}' not supported for stackable value of type {GetType()}");
                mergedValue = current.Param.Value;
                break;
        }

        return new Modification()
        {
            Param = (mergedValue, ModificationOperator.Set, current.Param.Mask | incoming.Param.Mask),
            Token = incoming.Token
        };
    }
}