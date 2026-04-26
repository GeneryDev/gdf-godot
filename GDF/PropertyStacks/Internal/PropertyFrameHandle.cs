using System;

namespace GDF.PropertyStacks.Internal;

public struct PropertyFrameHandle : IEquatable<PropertyFrameHandle>
{
    public readonly int Id;
    public string Description;
    public float Order;

    public PropertyFrameHandle(int id, string description, float order)
    {
        this.Id = id;
        Description = description;
        Order = order;
    }

    public bool Equals(PropertyFrameHandle other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object obj)
    {
        return obj is PropertyFrameHandle other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Id;
    }

    public static bool operator ==(PropertyFrameHandle left, PropertyFrameHandle right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PropertyFrameHandle left, PropertyFrameHandle right)
    {
        return !left.Equals(right);
    }
}