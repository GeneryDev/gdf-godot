using System.Collections.Generic;
using Godot;

namespace GDF.Logical.Values;

public interface IEnumerableValueSource<[MustBeVariant] out T>
{
    public IEnumerable<T> EnumerateValues(Node source);

    public sealed IEnumerable<TVariant> EnumerateValues<[MustBeVariant]TVariant>(Node source)
    {
        foreach (var value in EnumerateValues(source))
        {
            if (value is TVariant tv) yield return tv;
            else if (value is Variant variant) yield return variant.As<TVariant>();
        }
    } 
}