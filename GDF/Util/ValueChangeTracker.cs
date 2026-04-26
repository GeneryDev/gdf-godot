using System;

namespace GDF.Util;

public struct ValueChangeTracker<T> where T : IEquatable<T>
{
    public T LastTrackedValue;

    public bool HasChanged(T newValue)
    {
        if (!newValue.Equals(LastTrackedValue))
        {
            LastTrackedValue = newValue;
            return true;
        }

        return false;
    }
}