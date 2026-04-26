using System.Collections.Generic;
using Godot;

namespace GDF.Logical.Values;

[Tool]
[GlobalClass]
public abstract partial class ValueSource : Resource, IValueSource, IEnumerableValueSource<Variant>
{
    public abstract Variant GetValue(Node source);

    public T GetValue<[MustBeVariant] T>(Node source)
    {
        return GetValue(source).As<T>();
    }

    public IEnumerable<Variant> EnumerateValues(Node source)
    {
        var value = GetValue(source);
        if (value.VariantType == Variant.Type.Nil) yield break;
        
        if (value.VariantType == Variant.Type.Object && value.AsGodotObject() is IEnumerableValueSource<Variant> subEnumerable)
        {
            foreach (var subValue in subEnumerable.EnumerateValues(source))
            {
                yield return subValue;
            }
        }
        else
        {
            yield return value;
        }
    }

    public IEnumerable<TVariant> EnumerateValues<[MustBeVariant] TVariant>(Node source)
    {
        var enumerable = (IEnumerableValueSource<Variant>)this;
        return enumerable.EnumerateValues<TVariant>(source);
    }
}