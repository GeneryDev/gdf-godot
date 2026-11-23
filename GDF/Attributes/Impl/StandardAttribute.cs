using System;

namespace GDF.Attributes.Impl;

public abstract class StandardAttribute<T> : Attribute<T, ModificationOperation<T>>,
    IAttributeModifiable<T, ModificationOperation<T>> where T : IEquatable<T>
{
    public T BaseValue
    {
        get => _baseValue;
        set
        {
            if (_baseValue.Equals(value)) return;
            _baseValue = value;
            MarkDirty();
        }
    }
    public Func<T, T, T> DefaultFunction { get; set; }
    
    public override T CurrentValue => GetComputed().Param.AuxValue;
    
    private T _baseValue;

    public StandardAttribute() {}
    public StandardAttribute(T baseValue)
    {
        BaseValue = baseValue;
    }

    protected override ModificationOperation<T> GetInitialParam()
    {
        return new ModificationOperation<T>(BaseValue);
    }

    protected abstract T Lerp(T a, T b, float weight);

    protected override void Reduce(ref Modification current, in Modification incoming, float weight)
    {
        if (weight == 0) return;

        T targetValue;
        
        try
        {
            if (incoming.Param.Function != null)
            {
                // use incoming function
                targetValue = incoming.Param.Function.Invoke(current.Param.AuxValue, incoming.Param.AuxValue);
            }
            else if(DefaultFunction != null)
            {
                // use default function
                targetValue = DefaultFunction.Invoke(current.Param.AuxValue, incoming.Param.AuxValue);
            }
            else
            {
                // just set
                targetValue = incoming.Param.AuxValue;
            }
        }
        catch (NotSupportedException)
        {
            GdfLogger.Error($"Operation '{incoming.Param}' not supported for attribute of type {GetType()}");
            targetValue = incoming.Param.AuxValue;
        }

        T returnValue;
        if (weight == 1) returnValue = targetValue;
        else returnValue = Lerp(current.Param.AuxValue, targetValue, weight);

        current.Param = new ModificationOperation<T>(returnValue);
    }

    public static implicit operator T(StandardAttribute<T> attribute)
    {
        return attribute.CurrentValue;
    }

    ModificationOperation<T> IAttributeModifiable<T, ModificationOperation<T>>.InToParam(T value)
    {
        return new ModificationOperation<T>(DefaultFunction, value);
    }
}

public interface ITypedStandardAttribute<T> :
    IAttributeModifiable<T, ModificationOperation<T>>,
    IAttributeModifiable<Mod.Set<T>, ModificationOperation<T>>,
    IAttributeModifiable<Func<T, T, T>, ModificationOperation<T>>
    where T : IEquatable<T>
{
    ModificationOperation<T> IAttributeModifiable<T, ModificationOperation<T>>.InToParam(T value)
    {
        return new ModificationOperation<T>(value);
    }

    ModificationOperation<T> IAttributeModifiable<Mod.Set<T>, ModificationOperation<T>>.InToParam(Mod.Set<T> value)
    {
        return new ModificationOperation<T>((a, _) => a, value.Operand);
    }
    ModificationOperation<T> IAttributeModifiable<Func<T, T, T>, ModificationOperation<T>>.InToParam(Func<T, T, T> value)
    {
        return new ModificationOperation<T>(value);
    }
}

public readonly struct ModificationOperation<T>
{
    public readonly T AuxValue;
    public readonly Func<T, T, T> Function;

    public ModificationOperation(T auxValue)
    {
        AuxValue = auxValue;
    }

    public ModificationOperation(Func<T, T, T> function, T auxValue = default)
    {
        Function = function;
        AuxValue = auxValue;
    }
}

public static class Mod
{
    public readonly struct Set<T>(T operand)
    {
        public T Operand { get; } = operand;
    }

    public readonly struct Add<T>(T operand)
    {
        public T Operand { get; } = operand;
    }

    public readonly struct Multiply<T>(T operand)
    {
        public T Operand { get; } = operand;
    }

    public readonly struct And<T>(T operand)
    {
        public T Operand { get; } = operand;
    }

    public readonly struct Or<T>(T operand)
    {
        public T Operand { get; } = operand;
    }

    public readonly struct Blend<T>(T operand)
    {
        public T Operand { get; } = operand;
    }
}