using System;
using Godot;

// ReSharper disable once CheckNamespace
namespace GDF.Attributes.Impl;

public interface IVariantStandardAttribute<[MustBeVariant] T> :
    IAttributeModifiable<Variant, ModificationOperation<T>>,
    IAttributeModifiable<Mod.Set<Variant>, ModificationOperation<T>>,
    IAttributeModifiable<Func<Variant, Variant, Variant>, ModificationOperation<T>>
    where T : IEquatable<T>
{
    ModificationOperation<T> IAttributeModifiable<Variant, ModificationOperation<T>>.InToParam(Variant value)
    {
        return new ModificationOperation<T>(value.As<T>());
    }
    ModificationOperation<T> IAttributeModifiable<Mod.Set<Variant>, ModificationOperation<T>>.InToParam(Mod.Set<Variant> value)
    {
        return new ModificationOperation<T>((a, _) => a, value.Operand.As<T>());
    }
    ModificationOperation<T> IAttributeModifiable<Func<Variant, Variant, Variant>, ModificationOperation<T>>.InToParam(Func<Variant, Variant, Variant> value)
    {
        return new ModificationOperation<T>((tParam1, tParam2) => value.Invoke(Variant.From(tParam1), Variant.From(tParam2)).As<T>());
    }
}

public partial class FloatAttribute :
    IVariantStandardAttribute<float>,
    IAttributeModifiable<Mod.Add<Variant>, ModificationOperation<float>>,
    IAttributeModifiable<Mod.Multiply<Variant>, ModificationOperation<float>>
{
    ModificationOperation<float> IAttributeModifiable<Impl.Mod.Add<Variant>, ModificationOperation<float>>.InToParam(Impl.Mod.Add<Variant> value)
    {
        return new ModificationOperation<float>(Functions.Add, value.Operand.AsSingle());
    }

    ModificationOperation<float> IAttributeModifiable<Impl.Mod.Multiply<Variant>, ModificationOperation<float>>.InToParam(Impl.Mod.Multiply<Variant> value)
    {
        return new ModificationOperation<float>(Functions.Multiply, value.Operand.AsSingle());
    }
}

public partial class BoolAttribute :
    IVariantStandardAttribute<bool>,
    IAttributeModifiable<Mod.And<Variant>, ModificationOperation<bool>>,
    IAttributeModifiable<Mod.Or<Variant>, ModificationOperation<bool>>
{
    ModificationOperation<bool> IAttributeModifiable<Impl.Mod.And<Variant>, ModificationOperation<bool>>.InToParam(Impl.Mod.And<Variant> value)
    {
        return new ModificationOperation<bool>(Functions.And, value.Operand.AsBool());
    }

    ModificationOperation<bool> IAttributeModifiable<Impl.Mod.Or<Variant>, ModificationOperation<bool>>.InToParam(Impl.Mod.Or<Variant> value)
    {
        return new ModificationOperation<bool>(Functions.Or, value.Operand.AsBool());
    }
}

public partial class IntAttribute :
    IVariantStandardAttribute<int>,
    IAttributeModifiable<Mod.Add<Variant>, ModificationOperation<int>>,
    IAttributeModifiable<Mod.Multiply<Variant>, ModificationOperation<int>>,
    IAttributeModifiable<Mod.And<Variant>, ModificationOperation<int>>,
    IAttributeModifiable<Mod.Or<Variant>, ModificationOperation<int>>
{
    ModificationOperation<int> IAttributeModifiable<Impl.Mod.Add<Variant>, ModificationOperation<int>>.InToParam(Impl.Mod.Add<Variant> value)
    {
        return new ModificationOperation<int>(Functions.Add, value.Operand.AsInt32());
    }

    ModificationOperation<int> IAttributeModifiable<Impl.Mod.Multiply<Variant>, ModificationOperation<int>>.InToParam(Impl.Mod.Multiply<Variant> value)
    {
        return new ModificationOperation<int>(Functions.Multiply, value.Operand.AsInt32());
    }

    ModificationOperation<int> IAttributeModifiable<Impl.Mod.And<Variant>, ModificationOperation<int>>.InToParam(Impl.Mod.And<Variant> value)
    {
        return new ModificationOperation<int>(Functions.And, value.Operand.AsInt32());
    }

    ModificationOperation<int> IAttributeModifiable<Impl.Mod.Or<Variant>, ModificationOperation<int>>.InToParam(Impl.Mod.Or<Variant> value)
    {
        return new ModificationOperation<int>(Functions.Or, value.Operand.AsInt32());
    }
}

public class ColorAttribute : StandardAttribute<Color>,
    ITypedStandardAttribute<Color>,
    IVariantStandardAttribute<Color>,
    IAttributeModifiable<Mod.Add<Color>, ModificationOperation<Color>>,
    IAttributeModifiable<Mod.Multiply<Color>, ModificationOperation<Color>>,
    IAttributeModifiable<Mod.Blend<Color>, ModificationOperation<Color>>,
    IAttributeModifiable<Mod.Add<Variant>, ModificationOperation<Color>>,
    IAttributeModifiable<Mod.Multiply<Variant>, ModificationOperation<Color>>,
    IAttributeModifiable<Mod.Blend<Variant>, ModificationOperation<Color>>
{
    public ColorAttribute()
    {
        BaseValue = Colors.White;
    }
    public ColorAttribute(Color baseValue) : base(baseValue) {}

    protected override Color Lerp(Color a, Color b, float weight)
    {
        return a.Lerp(b, weight);
    }

    ModificationOperation<Color> IAttributeModifiable<Impl.Mod.Add<Color>, ModificationOperation<Color>>.InToParam(Impl.Mod.Add<Color> value)
    {
        return new ModificationOperation<Color>(Functions.Add, value.Operand);
    }

    ModificationOperation<Color> IAttributeModifiable<Impl.Mod.Multiply<Color>, ModificationOperation<Color>>.InToParam(Impl.Mod.Multiply<Color> value)
    {
        return new ModificationOperation<Color>(Functions.Multiply, value.Operand);
    }

    ModificationOperation<Color> IAttributeModifiable<Impl.Mod.Blend<Color>, ModificationOperation<Color>>.InToParam(Impl.Mod.Blend<Color> value)
    {
        return new ModificationOperation<Color>(Functions.Blend, value.Operand);
    }

    ModificationOperation<Color> IAttributeModifiable<Impl.Mod.Add<Variant>, ModificationOperation<Color>>.InToParam(Impl.Mod.Add<Variant> value)
    {
        return new ModificationOperation<Color>(Functions.Add, value.Operand.AsColor());
    }

    ModificationOperation<Color> IAttributeModifiable<Impl.Mod.Multiply<Variant>, ModificationOperation<Color>>.InToParam(Impl.Mod.Multiply<Variant> value)
    {
        return new ModificationOperation<Color>(Functions.Multiply, value.Operand.AsColor());
    }

    ModificationOperation<Color> IAttributeModifiable<Impl.Mod.Blend<Variant>, ModificationOperation<Color>>.InToParam(Impl.Mod.Blend<Variant> value)
    {
        return new ModificationOperation<Color>(Functions.Blend, value.Operand.AsColor());
    }
    
    public static class Functions
    {
        public static readonly Func<Color, Color, Color> Set = (_, incoming) => incoming;
        public static readonly Func<Color, Color, Color> Add = (current, incoming) => current + incoming;
        public static readonly Func<Color, Color, Color> Blend = (current, incoming) => incoming.Blend(current);
        public static readonly Func<Color, Color, Color> Multiply = (current, incoming) => current * incoming;

        public static Func<Color, Color, Color> FromName(string name)
        {
            return name switch
            {
                "Set" => Set,
                "Add" => Add,
                "Blend" => Blend,
                "Multiply" => Multiply,
                _ => null
            };
        }
    }
}

public class Vector2Attribute: StandardAttribute<Vector2>,
    ITypedStandardAttribute<Vector2>,
    IVariantStandardAttribute<Vector2>,
    IAttributeModifiable<Mod.Add<Vector2>, ModificationOperation<Vector2>>,
    IAttributeModifiable<Mod.Multiply<Vector2>, ModificationOperation<Vector2>>,
    IAttributeModifiable<Mod.Add<Variant>, ModificationOperation<Vector2>>,
    IAttributeModifiable<Mod.Multiply<Variant>, ModificationOperation<Vector2>>
{
    public Vector2Attribute() {}
    public Vector2Attribute(Vector2 baseValue) : base(baseValue) {}

    protected override Vector2 Lerp(Vector2 a, Vector2 b, float weight)
    {
        return a.Lerp(b, weight);
    }

    ModificationOperation<Vector2> IAttributeModifiable<Impl.Mod.Add<Vector2>, ModificationOperation<Vector2>>.InToParam(Impl.Mod.Add<Vector2> value)
    {
        return new ModificationOperation<Vector2>(Functions.Add, value.Operand);
    }

    ModificationOperation<Vector2> IAttributeModifiable<Impl.Mod.Multiply<Vector2>, ModificationOperation<Vector2>>.InToParam(Impl.Mod.Multiply<Vector2> value)
    {
        return new ModificationOperation<Vector2>(Functions.Multiply, value.Operand);
    }

    ModificationOperation<Vector2> IAttributeModifiable<Impl.Mod.Add<Variant>, ModificationOperation<Vector2>>.InToParam(Impl.Mod.Add<Variant> value)
    {
        return new ModificationOperation<Vector2>(Functions.Add, value.Operand.AsVector2());
    }

    ModificationOperation<Vector2> IAttributeModifiable<Impl.Mod.Multiply<Variant>, ModificationOperation<Vector2>>.InToParam(Impl.Mod.Multiply<Variant> value)
    {
        return new ModificationOperation<Vector2>(Functions.Multiply, value.Operand.AsVector2());
    }
    
    public static class Functions
    {
        public static readonly Func<Vector2, Vector2, Vector2> Set = (_, incoming) => incoming;
        public static readonly Func<Vector2, Vector2, Vector2> Add = (current, incoming) => current + incoming;
        public static readonly Func<Vector2, Vector2, Vector2> Multiply = (current, incoming) => current * incoming;

        public static Func<Vector2, Vector2, Vector2> FromName(string name)
        {
            return name switch
            {
                "Set" => Set,
                "Add" => Add,
                "Multiply" => Multiply,
                _ => null
            };
        }
    }
}

public class Vector3Attribute: StandardAttribute<Vector3>,
    ITypedStandardAttribute<Vector3>,
    IVariantStandardAttribute<Vector3>,
    IAttributeModifiable<Mod.Add<Vector3>, ModificationOperation<Vector3>>,
    IAttributeModifiable<Mod.Multiply<Vector3>, ModificationOperation<Vector3>>,
    IAttributeModifiable<Mod.Add<Variant>, ModificationOperation<Vector3>>,
    IAttributeModifiable<Mod.Multiply<Variant>, ModificationOperation<Vector3>>
{
    public Vector3Attribute() {}
    public Vector3Attribute(Vector3 baseValue) : base(baseValue) {}
    
    protected override Vector3 Lerp(Vector3 a, Vector3 b, float weight)
    {
        return a.Lerp(b, weight);
    }

    ModificationOperation<Vector3> IAttributeModifiable<Impl.Mod.Add<Vector3>, ModificationOperation<Vector3>>.InToParam(Impl.Mod.Add<Vector3> value)
    {
        return new ModificationOperation<Vector3>(Functions.Add, value.Operand);
    }

    ModificationOperation<Vector3> IAttributeModifiable<Impl.Mod.Multiply<Vector3>, ModificationOperation<Vector3>>.InToParam(Impl.Mod.Multiply<Vector3> value)
    {
        return new ModificationOperation<Vector3>(Functions.Multiply, value.Operand);
    }

    ModificationOperation<Vector3> IAttributeModifiable<Impl.Mod.Add<Variant>, ModificationOperation<Vector3>>.InToParam(Impl.Mod.Add<Variant> value)
    {
        return new ModificationOperation<Vector3>(Functions.Add, value.Operand.AsVector3());
    }

    ModificationOperation<Vector3> IAttributeModifiable<Impl.Mod.Multiply<Variant>, ModificationOperation<Vector3>>.InToParam(Impl.Mod.Multiply<Variant> value)
    {
        return new ModificationOperation<Vector3>(Functions.Multiply, value.Operand.AsVector3());
    }
    
    public static class Functions
    {
        public static readonly Func<Vector3, Vector3, Vector3> Set = (_, incoming) => incoming;
        public static readonly Func<Vector3, Vector3, Vector3> Add = (current, incoming) => current + incoming;
        public static readonly Func<Vector3, Vector3, Vector3> Multiply = (current, incoming) => current * incoming;

        public static Func<Vector3, Vector3, Vector3> FromName(string name)
        {
            return name switch
            {
                "Set" => Set,
                "Add" => Add,
                "Multiply" => Multiply,
                _ => null
            };
        }
    }
}

public class Vector4Attribute: StandardAttribute<Vector4>,
    ITypedStandardAttribute<Vector4>,
    IVariantStandardAttribute<Vector4>,
    IAttributeModifiable<Mod.Add<Vector4>, ModificationOperation<Vector4>>,
    IAttributeModifiable<Mod.Multiply<Vector4>, ModificationOperation<Vector4>>,
    IAttributeModifiable<Mod.Add<Variant>, ModificationOperation<Vector4>>,
    IAttributeModifiable<Mod.Multiply<Variant>, ModificationOperation<Vector4>>
{
    public Vector4Attribute() {}
    public Vector4Attribute(Vector4 baseValue) : base(baseValue) {}

    protected override Vector4 Lerp(Vector4 a, Vector4 b, float weight)
    {
        return a.Lerp(b, weight);
    }

    ModificationOperation<Vector4> IAttributeModifiable<Impl.Mod.Add<Vector4>, ModificationOperation<Vector4>>.InToParam(Impl.Mod.Add<Vector4> value)
    {
        return new ModificationOperation<Vector4>(Functions.Add, value.Operand);
    }

    ModificationOperation<Vector4> IAttributeModifiable<Impl.Mod.Multiply<Vector4>, ModificationOperation<Vector4>>.InToParam(Impl.Mod.Multiply<Vector4> value)
    {
        return new ModificationOperation<Vector4>(Functions.Multiply, value.Operand);
    }

    ModificationOperation<Vector4> IAttributeModifiable<Impl.Mod.Add<Variant>, ModificationOperation<Vector4>>.InToParam(Impl.Mod.Add<Variant> value)
    {
        return new ModificationOperation<Vector4>(Functions.Add, value.Operand.AsVector4());
    }

    ModificationOperation<Vector4> IAttributeModifiable<Impl.Mod.Multiply<Variant>, ModificationOperation<Vector4>>.InToParam(Impl.Mod.Multiply<Variant> value)
    {
        return new ModificationOperation<Vector4>(Functions.Multiply, value.Operand.AsVector4());
    }
    
    public static class Functions
    {
        public static readonly Func<Vector4, Vector4, Vector4> Set = (_, incoming) => incoming;
        public static readonly Func<Vector4, Vector4, Vector4> Add = (current, incoming) => current + incoming;
        public static readonly Func<Vector4, Vector4, Vector4> Multiply = (current, incoming) => current * incoming;

        public static Func<Vector4, Vector4, Vector4> FromName(string name)
        {
            return name switch
            {
                "Set" => Set,
                "Add" => Add,
                "Multiply" => Multiply,
                _ => null
            };
        }
    }
}

public class QuaternionAttribute: StandardAttribute<Quaternion>,
    ITypedStandardAttribute<Quaternion>,
    IVariantStandardAttribute<Quaternion>,
    IAttributeModifiable<Mod.Add<Quaternion>, ModificationOperation<Quaternion>>,
    IAttributeModifiable<Mod.Multiply<Quaternion>, ModificationOperation<Quaternion>>,
    IAttributeModifiable<Mod.Add<Variant>, ModificationOperation<Quaternion>>,
    IAttributeModifiable<Mod.Multiply<Variant>, ModificationOperation<Quaternion>>
{
    public QuaternionAttribute() : base(Quaternion.Identity) {}
    public QuaternionAttribute(Quaternion baseValue) : base(baseValue) {}
    
    protected override Quaternion Lerp(Quaternion a, Quaternion b, float weight)
    {
        return a.Slerp(b, weight);
    }

    ModificationOperation<Quaternion> IAttributeModifiable<Impl.Mod.Add<Quaternion>, ModificationOperation<Quaternion>>.InToParam(Impl.Mod.Add<Quaternion> value)
    {
        return new ModificationOperation<Quaternion>(Functions.Add, value.Operand);
    }

    ModificationOperation<Quaternion> IAttributeModifiable<Impl.Mod.Multiply<Quaternion>, ModificationOperation<Quaternion>>.InToParam(Impl.Mod.Multiply<Quaternion> value)
    {
        return new ModificationOperation<Quaternion>(Functions.Multiply, value.Operand);
    }

    ModificationOperation<Quaternion> IAttributeModifiable<Impl.Mod.Add<Variant>, ModificationOperation<Quaternion>>.InToParam(Impl.Mod.Add<Variant> value)
    {
        return new ModificationOperation<Quaternion>(Functions.Add, value.Operand.AsQuaternion());
    }

    ModificationOperation<Quaternion> IAttributeModifiable<Impl.Mod.Multiply<Variant>, ModificationOperation<Quaternion>>.InToParam(Impl.Mod.Multiply<Variant> value)
    {
        return new ModificationOperation<Quaternion>(Functions.Multiply, value.Operand.AsQuaternion());
    }
    
    public static class Functions
    {
        public static readonly Func<Quaternion, Quaternion, Quaternion> Set = (_, incoming) => incoming;
        public static readonly Func<Quaternion, Quaternion, Quaternion> Add = (current, incoming) => current + incoming;
        public static readonly Func<Quaternion, Quaternion, Quaternion> Multiply = (current, incoming) => current * incoming;

        public static Func<Quaternion, Quaternion, Quaternion> FromName(string name)
        {
            return name switch
            {
                "Set" => Set,
                "Add" => Add,
                "Multiply" => Multiply,
                _ => null
            };
        }
    }
}
