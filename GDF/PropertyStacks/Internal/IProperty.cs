using System.Collections.Generic;
using Godot;

namespace GDF.PropertyStacks.Internal;

public interface IProperty
{
    public bool ContainsHandle(PropertyFrameHandle handle);
    public void Set<T>(PropertyFrameHandle handle, T input);
    public void SetWeight(PropertyFrameHandle handle, float weight);
    public void Order(List<PropertyFrameHandle> handleOrder);
    public void InvalidateOrder();
    public object Compute();
    public int GetModCount();
    public Variant OutputToVariant(object value);
    public void CollectDebugInfoForHandle(PropertyFrameHandle handle, Godot.Collections.Dictionary dict);
    public bool IsInheritable();
    void CopyFrom(IProperty otherProperty, PropertyFrameHandle otherHandle, PropertyFrameHandle thisHandle);
}

public interface IProperty<out TOut>
{
    public TOut Compute();
    public TOut Recompute();
}