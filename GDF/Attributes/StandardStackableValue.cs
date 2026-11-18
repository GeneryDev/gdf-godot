using System;
using Godot;

namespace GDF.Attributes;

public abstract class StandardStackableValue<T>: StackableValue<T, ModificationOperation<T>>
{
    public override T CurrentValue => GetComputed().Param.Value;
    
    public ModificationOperator DefaultOperator { get; set; } = ModificationOperator.Set;

    public T BaseValue;

    public StandardStackableValue()
    {
    }

    public StandardStackableValue(T baseValue)
    {
        BaseValue = baseValue;
    }

    public abstract T ApplyOperation(T a, T b, ModificationOperator op);

    protected override ModificationOperation<T> GetInitialParam()
    {
        return new ModificationOperation<T>(BaseValue, ModificationOperator.Set);
    }

    protected abstract T Lerp(T a, T b, float weight);

    protected override Modification Reduce(in Modification current, in Modification incoming, float weight)
    {
        if (weight == 0) return current;

        T targetValue;
        
        try
        {
            targetValue = ApplyOperation(current.Param.Value, incoming.Param.Value, incoming.Param.Operator);
        }
        catch (NotSupportedException)
        {
            GD.PushError($"Operation '{incoming.Param}' not supported for stackable value of type {GetType()}");
            targetValue = incoming.Param.Value;
        }

        T returnValue;
        if (weight == 1) returnValue = targetValue;
        else returnValue = Lerp(current.Param.Value, targetValue, weight);

        return new Modification() { 
            Param = new ModificationOperation<T>(returnValue, ModificationOperator.Set)
        };
    }

    public static implicit operator T(StandardStackableValue<T> stackableValue)
    {
        return stackableValue.CurrentValue;
    }
}

public struct ModificationOperation<T>
{
    public T Value;
    public ModificationOperator Operator;

    public ModificationOperation(T value, ModificationOperator op)
    {
        Value = value;
        Operator = op;
    }
}

public enum ModificationOperator
{
    Set,
    Add,
    Multiply,
    And,
    Or
}

public static class StandardStackableValueExtensions
{
    public static void Default<T>(this StandardStackableValue<T> target, StackableValueToken token, T value)
    {
        target.Modify(token, new ModificationOperation<T>(value, target.DefaultOperator));
    }
    public static StackableValueTokenTyped<T, ModificationOperation<T>> Default<T>(this StackableValueTokenTyped<T, ModificationOperation<T>> token, T value)
    {
        if (token.Target is StandardStackableValue<T> standardTarget)
        {
            standardTarget.Set(token, value);
            return token;
        }
        else
        {
            throw new NotSupportedException("Called a StandardStackableValue extension method on a StackableValueTokenTyped value whose target is not a StandardStackableValue!");
        }
    }
    
    public static void Set<T>(this StandardStackableValue<T> target, StackableValueToken token, T value)
    {
        target.Modify(token, new ModificationOperation<T>(value, ModificationOperator.Set));
    }
    public static StackableValueTokenTyped<T, ModificationOperation<T>> Set<T>(this StackableValueTokenTyped<T, ModificationOperation<T>> token, T value)
    {
        return token.Modify(new ModificationOperation<T>(value, ModificationOperator.Set));
    }
    
    public static void Add<T>(this StandardStackableValue<T> target, StackableValueToken token, T value)
    {
        target.Modify(token, new ModificationOperation<T>(value, ModificationOperator.Add));
    }
    public static StackableValueTokenTyped<T, ModificationOperation<T>> Add<T>(this StackableValueTokenTyped<T, ModificationOperation<T>> token, T value)
    {
        return token.Modify(new ModificationOperation<T>(value, ModificationOperator.Add));
    }
    
    public static void Multiply<T>(this StandardStackableValue<T> target, StackableValueToken token, T value)
    {
        target.Modify(token, new ModificationOperation<T>(value, ModificationOperator.Multiply));
    }
    public static StackableValueTokenTyped<T, ModificationOperation<T>> Multiply<T>(this StackableValueTokenTyped<T, ModificationOperation<T>> token, T value)
    {
        return token.Modify(new ModificationOperation<T>(value, ModificationOperator.Multiply));
    }
    
    public static void And<T>(this StandardStackableValue<T> target, StackableValueToken token, T value)
    {
        target.Modify(token, new ModificationOperation<T>(value, ModificationOperator.And));
    }
    public static StackableValueTokenTyped<T, ModificationOperation<T>> And<T>(this StackableValueTokenTyped<T, ModificationOperation<T>> token, T value)
    {
        return token.Modify(new ModificationOperation<T>(value, ModificationOperator.And));
    }
    
    public static void Or<T>(this StandardStackableValue<T> target, StackableValueToken token, T value)
    {
        target.Modify(token, new ModificationOperation<T>(value, ModificationOperator.Or));
    }
    public static StackableValueTokenTyped<T, ModificationOperation<T>> Or<T>(this StackableValueTokenTyped<T, ModificationOperation<T>> token, T value)
    {
        return token.Modify(new ModificationOperation<T>(value, ModificationOperator.Or));
    }
}