using System;
using System.Collections.Generic;
using Godot;

namespace GDF.PropertyStacks.Internal;

public class PropertyImpl<TMed, TOut, TCache> : IProperty, IProperty<TOut>
{
    private readonly IPropertyDefinition<TMed, TOut, TCache> _def;
    private readonly TCache _cache;
    private readonly List<PropertyFrameEntry> _entries;
    private TOut _computedValue;

    private int _modCount;
    private bool _orderDirty = true;
    private bool _computedValueDirty = true;

    public PropertyImpl(IPropertyDefinition<TMed, TOut, TCache> def)
    {
        _def = def;
        _entries = new List<PropertyFrameEntry>();
        _cache = _def.CreateCache();
    }

    public void Set<T>(PropertyFrameHandle handle, T input)
    {
        var asTMed = _def.InputToIntermediate(input);
        Set(handle, asTMed);
    }

    private void Set(PropertyFrameHandle handle, TMed med)
    {
        int existingIndex = EntryIndexOfHandle(handle);
        var entry = existingIndex >= 0 ? _entries[existingIndex] : new PropertyFrameEntry()
        {
            FrameHandle = handle,
            Weight = 1.0f
        };
        entry.Value = med;
        if (existingIndex >= 0)
        {
            _entries[existingIndex] = entry;
        }
        else
        {
            _entries.Add(entry);
            _orderDirty = true;
        }

        _computedValueDirty = true;
        _modCount++;
    }

    public void SetWeight(PropertyFrameHandle handle, float weight)
    {
        int existingIndex = EntryIndexOfHandle(handle);
        if (existingIndex < 0)
        {
            throw new ArgumentException("Cannot set weight for property handle not present in the stack!");
        }
        var entry = _entries[existingIndex];
        entry.Weight = weight;
        _entries[existingIndex] = entry;
        _computedValueDirty = true;
        _modCount++;
    }

    public void InvalidateOrder()
    {
        _orderDirty = true;
    }

    public void Order(List<PropertyFrameHandle> handleOrder)
    {
        if (!_orderDirty) return;
        var prevEntryArray = _entries.ToArray();
        var prevEntryArrayLength = prevEntryArray.Length;
        
        _entries.Clear();
        for (var frameIndex = 0; frameIndex < handleOrder.Count; frameIndex++)
        {
            var handle = handleOrder[frameIndex];
            
            for (var entryIndex = 0; entryIndex < prevEntryArrayLength; entryIndex++)
            {
                var entry = prevEntryArray[entryIndex];
                if (entry.FrameHandle == handle)
                {
                    _entries.Add(entry);

                    prevEntryArray[entryIndex] = prevEntryArray[prevEntryArrayLength - 1];
                    prevEntryArrayLength--;
                    break;
                }
            }
        }
        
        // Side effect: Any entries belonging to handles no longer in the stack are removed,
        // as we only re-add the entries if they exist in the given order list.
        _orderDirty = false;
        _computedValueDirty = true;
        _modCount++;
    }

    object IProperty.Compute()
    {
        return Compute();
    }

    public TOut Compute()
    {
        return _computedValueDirty ? Recompute() : _computedValue;
    }

    public TOut Recompute()
    {
        if (_orderDirty)
        {
            throw new Exception("Cannot compute value for property when the order hasn't been updated");
        }
        var currentValue = _def.GetInitialValue(_cache);
        foreach (var entry in _entries)
        {
            currentValue = _def.Reduce(currentValue, entry.Value, entry.Weight, entry.FrameHandle);
        }

        TOut finalValue = _def.IntermediateToOutput(currentValue);
        _computedValue = finalValue;
        _computedValueDirty = false;
        return finalValue;
    }

    public int GetModCount()
    {
        return _modCount;
    }

    public bool IsInheritable()
    {
        return _def.IsInheritable();
    }

    public Variant OutputToVariant(object value)
    {
        return _def.OutputToVariant((TOut)value);
    }

    private int EntryIndexOfHandle(PropertyFrameHandle handle)
    {
        for (var i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].FrameHandle == handle) return i;
        }

        return -1;
    }

    public bool ContainsHandle(PropertyFrameHandle handle)
    {
        return EntryIndexOfHandle(handle) != -1;
    }

    public void CopyFrom(IProperty otherProperty, PropertyFrameHandle otherHandle, PropertyFrameHandle thisHandle)
    {
        if (otherProperty is not PropertyImpl<TMed, TOut, TCache> otherProp)
        {
            throw new ArgumentException("Cannot copy property stack handle, types incompatible!");
        }

        int otherIndex = otherProp.EntryIndexOfHandle(otherHandle);
        if (otherIndex == -1) return;
        var otherEntry = otherProp._entries[otherIndex];
        this.Set(thisHandle, otherEntry.Value);
        this.SetWeight(thisHandle, otherEntry.Weight);
    }

    public void CollectDebugInfoForHandle(PropertyFrameHandle handle, Godot.Collections.Dictionary dict)
    {
        for (var i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            if (entry.FrameHandle == handle)
            {
                var propertyId = _def is PropertyDefinitionResource def ? def.PropertyId : _def.ToString();
                var entryDictionary = new Godot.Collections.Dictionary();
                dict[propertyId] = entryDictionary;

                entryDictionary["value"] = _def.IntermediateToDebug(entry.Value);
                entryDictionary["weight"] = entry.Weight;
                break;
            }
        }
    }

    private struct PropertyFrameEntry
    {
        public TMed Value;
        public PropertyFrameHandle FrameHandle;
        public float Weight;
    }
}