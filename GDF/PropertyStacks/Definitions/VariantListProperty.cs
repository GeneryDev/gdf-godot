using System;
using System.Collections.Generic;
using GDF.PropertyStacks.Internal;
using Godot;

namespace GDF.PropertyStacks.Definitions;

public partial class VariantListProperty<T, TMeta> : PropertyDefinitionResource, IPropertyDefinition<VariantListEntry<T, TMeta>, VariantListOutput<T, TMeta>, List<T>>
{
    public virtual VariantListEntry<T, TMeta> GetInitialValue(List<T> cache)
    {
        cache.Clear();
        return new VariantListEntry<T, TMeta>() {Group = 0, List = cache};
    }

    public virtual VariantListEntry<T, TMeta> InputToIntermediate(object input)
    {
        switch (input)
        {
            case VariantListEntry<T, TMeta> listT:
                return listT;
            case Variant v:
            {
                if (VariantIsT(v, out var t))
                {
                    return VariantListEntry.Create<T, TMeta>(t, 0);
                }
                else
                {
                    return new VariantListEntry<T, TMeta>(); // empty
                }
            }
            case T t:
                return VariantListEntry.Create<T, TMeta>(t, 0);
            default:
                throw new ArgumentException($"Unsupported object of type {input?.GetType()} in variant list property {PropertyId}",
                    nameof(input));
        }
    }

    public virtual VariantListEntry<T, TMeta> Reduce(VariantListEntry<T, TMeta> lower, VariantListEntry<T, TMeta> higher, float weight,
        PropertyFrameHandle handle)
    {
        if (weight == 0) return lower;
        
        if(higher.Group < lower.Group) return lower;
        if(higher.Group > lower.Group) lower.List.Clear();
        ReduceMeta(ref lower, ref higher, weight, ref handle);
        lower.AddFrom(higher, this);

        return lower;
    }

    protected virtual void ReduceMeta(ref VariantListEntry<T, TMeta> lower, ref VariantListEntry<T, TMeta> higher, float weight,
        ref PropertyFrameHandle handle)
    {
    }

    public virtual VariantListOutput<T, TMeta> IntermediateToOutput(VariantListEntry<T, TMeta> value)
    {
        return new VariantListOutput<T, TMeta>() { List = value.List };
    }

    public virtual Variant OutputToVariant(VariantListOutput<T, TMeta> value)
    {
        var arr = new Godot.Collections.Array();
        if(value.List != null)
            arr.AddRange(value.List);
        return arr;
    }

    public override IProperty CreateProperty()
    {
        return new PropertyImpl<VariantListEntry<T, TMeta>, VariantListOutput<T, TMeta>, List<T>>(this);
    }

    public List<T> CreateCache()
    {
        return new List<T>();
    }

    public virtual bool VariantIsT(Variant v, out T converted)
    {
        if (v.VariantType == Variant.Type.Object && v.AsGodotObject() is T t)
        {
            converted = t;
            return true;
        }

        converted = default;
        return false;
    }
}

public static class VariantListEntry
{
    public static VariantListEntry<T, TMeta> Create<T, TMeta>(List<T> list, int group, TMeta meta = default)
    {
        return new VariantListEntry<T, TMeta>()
        {
            Group = group,
            List = list,
            Meta = meta
        };
    }
    public static VariantListEntry<T, TMeta> Create<[MustBeVariant]T, TMeta>(Godot.Collections.Array arr, int group, TMeta meta = default)
    {
        return new VariantListEntry<T, TMeta>()
        {
            Group = group,
            InputVariantArray = arr,
            Meta = meta
        };
    }
    public static VariantListEntry<T, TMeta> Create<T, TMeta>(T entry, int group, TMeta meta = default)
    {
        return new VariantListEntry<T, TMeta>()
        {
            Group = group,
            InputVariantSingleEntry = entry,
            Meta = meta
        };
    }
    
    public static VariantListEntry<T, Empty> Create<T>(List<T> list, int group)
    {
        return Create<T, Empty>(list, group, default);
    }
    public static VariantListEntry<T, Empty> Create<[MustBeVariant]T>(Godot.Collections.Array arr, int group)
    {
        return Create<T, Empty>(arr, group, default);
    }
    public static VariantListEntry<T, Empty> Create<T>(T entry, int group)
    {
        return Create<T, Empty>(entry, group, default);
    }
}

#nullable enable
public struct VariantListEntry<T, TMeta>
{
    public int Group;
    public List<T> List;
    public Godot.Collections.Array InputVariantArray;
    public T? InputVariantSingleEntry;
    
    public TMeta Meta;

    public bool AddFrom(VariantListEntry<T, TMeta> higher, VariantListProperty<T, TMeta> property)
    {
        if(higher.List is {Count: > 0})
            this.List.AddRange(higher.List);
        if (higher.InputVariantArray is { Count: > 0 })
        {
            foreach (var entry in higher.InputVariantArray)
            {
                if(property.VariantIsT(entry, out var t))
                    this.List.Add(t);
            }
        }
        if (higher.InputVariantSingleEntry != null)
        {
            this.List.Add(higher.InputVariantSingleEntry);
        }

        this.Group = higher.Group;
        return true;
    }
}
#nullable restore

public struct VariantListOutput<T, TMeta>
{
    public List<T> List;
    public TMeta Meta;
}