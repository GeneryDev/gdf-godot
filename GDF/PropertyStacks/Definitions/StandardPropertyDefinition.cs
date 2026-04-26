using System;
using System.Runtime.InteropServices;
using GDF.PropertyStacks.Internal;
using Godot;
using Godot.Collections;

namespace GDF.PropertyStacks.Definitions;

public abstract partial class StandardPropertyDefinition<[MustBeVariant] T> : PropertyDefinitionResource, IPropertyDefinition<VectorModification<T>, T, Empty>
{
    [Export] public ModificationOperation DefaultOperator;
    
    public abstract T GetDefaultValue();
    
    public virtual T ApplyAdd(T a, T b)
    {
        throw new NotSupportedException();
    }

    public virtual T ApplyMultiply(T a, T b)
    {
        throw new NotSupportedException();
    }

    public virtual T ApplyOr(T a, T b)
    {
        throw new NotSupportedException();
    }

    public virtual T ApplyAnd(T a, T b)
    {
        throw new NotSupportedException();
    }

    public abstract T Lerp(T a, T b, float weight);


    public virtual VectorModification<T> GetInitialValue(Empty cache)
    {
        return new VectorModification<T>() { Value = GetDefaultValue(), Operation = DefaultOperator};
    }

    public virtual VectorModification<T> InputToIntermediate(object input)
    {
        return input switch
        {
            VectorModification<T> modT => modT,
            VectorModification<Variant> modV => new VectorModification<T>() {Value = modV.Value.As<T>(), Operation = modV.Operation},
            Variant v => new VectorModification<T>() { Value = v.As<T>(), Operation = DefaultOperator },
            T t => new VectorModification<T>() { Value = t, Operation = DefaultOperator},
            _ => throw new ArgumentException($"Unsupported object of type {input?.GetType()} in vector property {PropertyId}", nameof(input))
        };
    }

    public virtual VectorModification<T> Reduce(VectorModification<T> lower, VectorModification<T> higher, float weight,
        PropertyFrameHandle handle)
    {
        if (weight == 0) return lower;
        
        T targetValue;
        try
        {
            targetValue = higher.Operation switch
            {
                ModificationOperation.Set => higher.Value,
                ModificationOperation.Add => ApplyAdd(lower.Value, higher.Value),
                ModificationOperation.Multiply => ApplyMultiply(lower.Value, higher.Value),
                ModificationOperation.Or => ApplyOr(lower.Value, higher.Value),
                ModificationOperation.And => ApplyAnd(lower.Value, higher.Value),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        catch (NotSupportedException)
        {
            targetValue = higher.Value;
            GD.PushError($"Operation '{higher.Operation}' not supported for property definition of type {GetType()}");
        }

        T returnValue;
        if (weight == 1) returnValue = targetValue;
        else returnValue = Lerp(lower.Value, targetValue, weight);
        
        return new VectorModification<T>() { Value = returnValue };
    }

    public virtual T IntermediateToOutput(VectorModification<T> value)
    {
        return value.Value;
    }

    public abstract Variant OutputToVariant(T value);

    public Variant IntermediateToDebug(VectorModification<T> value)
    {
        var obj = new Dictionary();
        obj["value"] = OutputToVariant(value.Value);
        obj["operation"] = typeof(ModificationOperation).GetEnumName(value.Operation);
        return obj;
    }

    public override IProperty CreateProperty()
    {
        return new PropertyImpl<VectorModification<T>, T, Empty>(this);
    }

    public Empty CreateCache()
    {
        return new Empty();
    }
}

public struct VectorModification<T>
{
    public ModificationOperation Operation = ModificationOperation.Set;
    public T Value;

    public VectorModification()
    {
        Value = default;
    }

    public static VectorModification<T> Set(T value)
    {
        return new VectorModification<T>()
        {
            Operation = ModificationOperation.Set,
            Value = value
        };
    }

    public static VectorModification<T> Add(T value)
    {
        return new VectorModification<T>()
        {
            Operation = ModificationOperation.Add,
            Value = value
        };
    }

    public static VectorModification<T> Multiply(T value)
    {
        return new VectorModification<T>()
        {
            Operation = ModificationOperation.Multiply,
            Value = value
        };
    }

    public static VectorModification<T> Or(T value)
    {
        return new VectorModification<T>()
        {
            Operation = ModificationOperation.Or,
            Value = value
        };
    }

    public static VectorModification<T> And(T value)
    {
        return new VectorModification<T>()
        {
            Operation = ModificationOperation.And,
            Value = value
        };
    }
}

public enum ModificationOperation
{
    Set,
    Add,
    Multiply,
    And,
    Or
}

[StructLayout(LayoutKind.Explicit)]
public struct Empty {}