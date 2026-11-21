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

    protected override Modification Reduce(in Modification current, in Modification incoming, float weight)
    {
        if (weight == 0) return current;

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

        return new Modification() { 
            Param = new ModificationOperation<T>(returnValue)
        };
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

public struct ModificationOperation<T>
{
    public T AuxValue;
    public Func<T, T, T> Function;

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

public static class StandardAttributeExtensions
{
    public static void Default<T>(this StandardAttribute<T> target, AttributeModifierToken token, T value) where T : IEquatable<T>
    {
        target.Modify(token, new ModificationOperation<T>(value));
    }
    public static AttributeModifierTokenTyped<TAttr> Default<TAttr, TValue>(this AttributeModifierTokenTyped<TAttr> token, TValue value)
        where TAttr : StandardAttribute<TValue>
        where TValue : IEquatable<TValue>
    {
        token.Target.Modify(token, new ModificationOperation<TValue>(value));
        return token;
    }

    
    public static void Set<TAttr, TValue>(this TAttr target, AttributeModifierToken token, TValue value)
        where TAttr : IAttributeModifiable<TValue, ModificationOperation<TValue>>
        where TValue : IEquatable<TValue>
    {
        target.Modify(token, value);
    }
    public static AttributeModifierTokenTyped<TAttr> Set<TAttr, TValue>(this AttributeModifierTokenTyped<TAttr> token, TValue value)
        where TAttr : IAttribute, IAttributeModifiable<Mod.Set<TValue>>
        where TValue : IEquatable<TValue>
    {
        token.Modify(new Mod.Set<TValue>());
        return token;
    }

    
    public static void Add<TAttr, TValue>(this TAttr target, AttributeModifierToken token, TValue value)
        where TAttr : IAttribute, IAttributeModifiable<Mod.Add<TValue>>
        where TValue : IEquatable<TValue>
    {
        target.Modify(token, new Mod.Add<TValue>(value));
    }
    public static AttributeModifierTokenTyped<TAttr> Add<TAttr, TValue>(this AttributeModifierTokenTyped<TAttr> token, TValue value)
        where TAttr : IAttribute, IAttributeModifiable<Mod.Add<TValue>>
        where TValue : IEquatable<TValue>
    {
        token.Modify(new Mod.Add<TValue>(value));
        return token;
    }

    
    public static void Multiply<TAttr, TValue>(this TAttr target, AttributeModifierToken token, TValue value)
        where TAttr : IAttribute, IAttributeModifiable<Mod.Multiply<TValue>>
        where TValue : IEquatable<TValue>
    {
        target.Modify(token, new Mod.Multiply<TValue>(value));
    }
    public static AttributeModifierTokenTyped<TAttr> Multiply<TAttr, TValue>(this AttributeModifierTokenTyped<TAttr> token, TValue value)
        where TAttr : IAttribute, IAttributeModifiable<Mod.Multiply<TValue>>
        where TValue : IEquatable<TValue>
    {
        token.Modify(new Mod.Multiply<TValue>(value));
        return token;
    }

    
    public static void And<TAttr, TValue>(this TAttr target, AttributeModifierToken token, TValue value)
        where TAttr : IAttribute, IAttributeModifiable<Mod.And<TValue>>
        where TValue : IEquatable<TValue>
    {
        target.Modify(token, new Mod.And<TValue>(value));
    }
    public static AttributeModifierTokenTyped<TAttr> And<TAttr, TValue>(this AttributeModifierTokenTyped<TAttr> token, TValue value)
        where TAttr : IAttribute, IAttributeModifiable<Mod.And<TValue>>
        where TValue : IEquatable<TValue>
    {
        token.Modify(new Mod.And<TValue>(value));
        return token;
    }

    
    public static void Or<TAttr, TValue>(this TAttr target, AttributeModifierToken token, TValue value)
        where TAttr : IAttribute, IAttributeModifiable<Mod.Or<TValue>>
        where TValue : IEquatable<TValue>
    {
        target.Modify(token, new Mod.Or<TValue>(value));
    }
    public static AttributeModifierTokenTyped<TAttr> Or<TAttr, TValue>(this AttributeModifierTokenTyped<TAttr> token, TValue value)
        where TAttr : IAttribute, IAttributeModifiable<Mod.Or<TValue>>
        where TValue : IEquatable<TValue>
    {
        token.Modify(new Mod.Or<TValue>(value));
        return token;
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