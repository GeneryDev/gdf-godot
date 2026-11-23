using System;
using Godot;

namespace GDF.Attributes.Impl;

public abstract class StandardAttribute<T, TChain> :
    Attribute<T, ModificationOperation<T>, TChain>,
    IModifiable<T>
    where T : IEquatable<T>
    where TChain :
        StandardModifierChain<T>,
        new()
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
    public Func<T, T, T> DefaultFunction
    {
        get => ModifierChain.DefaultFunction;
        set => ModifierChain.DefaultFunction = value;
    }

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

    public static implicit operator T(StandardAttribute<T, TChain> attribute)
    {
        return attribute.CurrentValue;
    }

    public void Modify(ModifierToken token, T value)
    {
        ModifierChain.Modify(token, value);
    }
}

public abstract class StandardModifierChain<T> :
    ModifierChain<ModificationOperation<T>>,
    IModifiable<T, ModificationOperation<T>>
    where T :
        IEquatable<T>
{
    public Func<T, T, T> DefaultFunction { get; set; }
    
    protected abstract T Lerp(T a, T b, float weight);

    protected override void Reduce(ref Step current, in Step incoming, float weight)
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

    ModificationOperation<T> IModifiable<T, ModificationOperation<T>>.InToParam(T value)
    {
        return new ModificationOperation<T>(DefaultFunction, value);
    }
}

public interface ITypedStandardAttribute<T, TChain> :
    IAttribute<TChain>,
    IModifiable<T>,
    IModifiable<Mod.Set<T>>,
    IModifiable<Func<T, T, T>>
    where T : IEquatable<T>
    where TChain :
        StandardModifierChain<T>,
        IModifiable<Mod.Set<T>>,
        IModifiable<Func<T, T, T>>
{
    void IModifiable<T>.Modify(ModifierToken token, T value)
    {
        GetModifierChain().Modify(token, value);
    }

    void IModifiable<Mod.Set<T>>.Modify(ModifierToken token, Mod.Set<T> value)
    {
        GetModifierChain().Modify(token, value);
    }

    void IModifiable<Func<T, T, T>>.Modify(ModifierToken token, Func<T, T, T> value)
    {
        GetModifierChain().Modify(token, value);
    }
}

public interface ITypedStandardModifierChain<T> :
    IModifiable<T, ModificationOperation<T>>,
    IModifiable<Mod.Set<T>, ModificationOperation<T>>,
    IModifiable<Func<T, T, T>, ModificationOperation<T>>,
    
    IModifierComputable<T>

    where T : IEquatable<T>
{
    ModificationOperation<T> IModifiable<T, ModificationOperation<T>>.InToParam(T value)
    {
        return new ModificationOperation<T>(value);
    }
    ModificationOperation<T> IModifiable<Mod.Set<T>, ModificationOperation<T>>.InToParam(Mod.Set<T> value)
    {
        return new ModificationOperation<T>((a, _) => a, value.Operand);
    }
    ModificationOperation<T> IModifiable<Func<T, T, T>, ModificationOperation<T>>.InToParam(Func<T, T, T> value)
    {
        return new ModificationOperation<T>(value);
    }

    T IModifierComputable<T>.Compute(in T initial)
    {
        return ((StandardModifierChain<T>)this).Compute(new StandardModifierChain<T>.Step()
        {
            Param = new ModificationOperation<T>(initial)
        }).Param.AuxValue;
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