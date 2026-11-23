using System;
using Godot;

// ReSharper disable once CheckNamespace
namespace GDF.Attributes.Impl;

public interface IVariantStandardAttribute<[MustBeVariant] T, TChain> :
    IAttribute<TChain>,
    IModifiable<Variant>,
    IModifiable<Mod.Set<Variant>>,
    IModifiable<Func<Variant, Variant, Variant>>
    where T : IEquatable<T>
    where TChain :
        StandardModifierChain<T>,
        IModifiable<Variant>,
        IModifiable<Mod.Set<Variant>>,
        IModifiable<Func<Variant, Variant, Variant>>
{
    void IModifiable<Variant>.Modify(ModifierToken token, Variant value)
    {
        GetModifierChain().Modify(token, value);
    }

    void IModifiable<Mod.Set<Variant>>.Modify(ModifierToken token, Mod.Set<Variant> value)
    {
        GetModifierChain().Modify(token, value);
    }

    void IModifiable<Func<Variant, Variant, Variant>>.Modify(ModifierToken token, Func<Variant, Variant, Variant> value)
    {
        GetModifierChain().Modify(token, value);
    }
}

public interface IVariantStandardModifierChain<[MustBeVariant] T> :
    IModifiable<Variant, ModificationOperation<T>>,
    IModifiable<Mod.Set<Variant>, ModificationOperation<T>>,
    IModifiable<Func<Variant, Variant, Variant>, ModificationOperation<T>>,
    
    IModifierComputable<Variant>

    where T : IEquatable<T>
{
    ModificationOperation<T> IModifiable<Variant, ModificationOperation<T>>.InToParam(Variant value)
    {
        return new ModificationOperation<T>(value.As<T>());
    }
    ModificationOperation<T> IModifiable<Mod.Set<Variant>, ModificationOperation<T>>.InToParam(Mod.Set<Variant> value)
    {
        return new ModificationOperation<T>((a, _) => a, value.Operand.As<T>());
    }
    ModificationOperation<T> IModifiable<Func<Variant, Variant, Variant>, ModificationOperation<T>>.InToParam(Func<Variant, Variant, Variant> value)
    {
        return new ModificationOperation<T>((tParam1, tParam2) => value.Invoke(Variant.From(tParam1), Variant.From(tParam2)).As<T>());
    }

    Variant IModifierComputable<Variant>.Compute(in Variant initial)
    {
        return Variant.From(((StandardModifierChain<T>)this).Compute(new StandardModifierChain<T>.Step()
        {
            // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
            Param = new ModificationOperation<T>(initial.As<T>())
        }).Param.AuxValue);
    }
}

public partial class FloatAttribute :
    IModifiable<Mod.Add<Variant>>,
    IModifiable<Mod.Multiply<Variant>>
{
    public void Modify(ModifierToken token, Mod.Add<Variant> value)
    {
        GetModifierChain().Modify(token, value);
    }

    public void Modify(ModifierToken token, Mod.Multiply<Variant> value)
    {
        GetModifierChain().Modify(token, value);
    }
}
public partial class FloatModifierChain :
    IVariantStandardModifierChain<float>,
    IModifiable<Mod.Add<Variant>, ModificationOperation<float>>,
    IModifiable<Mod.Multiply<Variant>, ModificationOperation<float>>
{
    ModificationOperation<float> IModifiable<Mod.Add<Variant>, ModificationOperation<float>>.InToParam(Mod.Add<Variant> value)
    {
        return new ModificationOperation<float>(Functions.Add, value.Operand.AsSingle());
    }

    ModificationOperation<float> IModifiable<Mod.Multiply<Variant>, ModificationOperation<float>>.InToParam(Mod.Multiply<Variant> value)
    {
        return new ModificationOperation<float>(Functions.Multiply, value.Operand.AsSingle());
    }
}

public partial class BoolAttribute :
    IModifiable<Mod.And<Variant>>,
    IModifiable<Mod.Or<Variant>>
{
    public void Modify(ModifierToken token, Mod.And<Variant> value)
    {
        GetModifierChain().Modify(token, value);
    }

    public void Modify(ModifierToken token, Mod.Or<Variant> value)
    {
        GetModifierChain().Modify(token, value);
    }
}
public partial class BoolModifierChain :
    IVariantStandardModifierChain<bool>,
    IModifiable<Mod.And<Variant>, ModificationOperation<bool>>,
    IModifiable<Mod.Or<Variant>, ModificationOperation<bool>>
{
    ModificationOperation<bool> IModifiable<Mod.And<Variant>, ModificationOperation<bool>>.InToParam(Mod.And<Variant> value)
    {
        return new ModificationOperation<bool>(Functions.And, value.Operand.AsBool());
    }

    ModificationOperation<bool> IModifiable<Mod.Or<Variant>, ModificationOperation<bool>>.InToParam(Mod.Or<Variant> value)
    {
        return new ModificationOperation<bool>(Functions.Or, value.Operand.AsBool());
    }
}

public partial class IntAttribute :
    IModifiable<Mod.Add<Variant>>,
    IModifiable<Mod.Multiply<Variant>>,
    IModifiable<Mod.And<Variant>>,
    IModifiable<Mod.Or<Variant>>
{
    public void Modify(ModifierToken token, Mod.Add<Variant> value)
    {
        GetModifierChain().Modify(token, value);
    }

    public void Modify(ModifierToken token, Mod.Multiply<Variant> value)
    {
        GetModifierChain().Modify(token, value);
    }

    public void Modify(ModifierToken token, Mod.And<Variant> value)
    {
        GetModifierChain().Modify(token, value);
    }

    public void Modify(ModifierToken token, Mod.Or<Variant> value)
    {
        GetModifierChain().Modify(token, value);
    }
}
public partial class IntModifierChain :
    IVariantStandardModifierChain<int>,
    IModifiable<Mod.Add<Variant>, ModificationOperation<int>>,
    IModifiable<Mod.Multiply<Variant>, ModificationOperation<int>>,
    IModifiable<Mod.And<Variant>, ModificationOperation<int>>,
    IModifiable<Mod.Or<Variant>, ModificationOperation<int>>
{
    ModificationOperation<int> IModifiable<Mod.Add<Variant>, ModificationOperation<int>>.InToParam(Mod.Add<Variant> value)
    {
        return new ModificationOperation<int>(Functions.Add, value.Operand.AsInt32());
    }

    ModificationOperation<int> IModifiable<Mod.Multiply<Variant>, ModificationOperation<int>>.InToParam(Mod.Multiply<Variant> value)
    {
        return new ModificationOperation<int>(Functions.Multiply, value.Operand.AsInt32());
    }

    ModificationOperation<int> IModifiable<Mod.And<Variant>, ModificationOperation<int>>.InToParam(Mod.And<Variant> value)
    {
        return new ModificationOperation<int>(Functions.And, value.Operand.AsInt32());
    }

    ModificationOperation<int> IModifiable<Mod.Or<Variant>, ModificationOperation<int>>.InToParam(Mod.Or<Variant> value)
    {
        return new ModificationOperation<int>(Functions.Or, value.Operand.AsInt32());
    }
}

public partial class ColorAttribute :
    StandardAttribute<Color, ColorModifierChain>,
    IModifiable<Mod.Add<Color>>,
    IModifiable<Mod.Multiply<Color>>,
    IModifiable<Mod.Blend<Color>>,
    IModifiable<Mod.Add<Variant>>,
    IModifiable<Mod.Multiply<Variant>>,
    IModifiable<Mod.Blend<Variant>>
{
    public ColorAttribute()
    {
        BaseValue = Colors.White;
    }
    public ColorAttribute(Color baseValue) : base(baseValue) {}
    
    public abstract class Functions : ColorModifierChain.Functions;

    public void Modify(ModifierToken token, Mod.Add<Color> value)
    {
        GetModifierChain().Modify(token, value);
    }

    public void Modify(ModifierToken token, Mod.Multiply<Color> value)
    {
        GetModifierChain().Modify(token, value);
    }

    public void Modify(ModifierToken token, Mod.Blend<Color> value)
    {
        GetModifierChain().Modify(token, value);
    }

    public void Modify(ModifierToken token, Mod.Add<Variant> value)
    {
        GetModifierChain().Modify(token, value);
    }

    public void Modify(ModifierToken token, Mod.Multiply<Variant> value)
    {
        GetModifierChain().Modify(token, value);
    }

    public void Modify(ModifierToken token, Mod.Blend<Variant> value)
    {
        GetModifierChain().Modify(token, value);
    }
}
public class ColorModifierChain : StandardModifierChain<Color>,
    ITypedStandardModifierChain<Color>,
    IVariantStandardModifierChain<Color>,
    IModifiable<Mod.Add<Color>, ModificationOperation<Color>>,
    IModifiable<Mod.Multiply<Color>, ModificationOperation<Color>>,
    IModifiable<Mod.Blend<Color>, ModificationOperation<Color>>,
    IModifiable<Mod.Add<Variant>, ModificationOperation<Color>>,
    IModifiable<Mod.Multiply<Variant>, ModificationOperation<Color>>,
    IModifiable<Mod.Blend<Variant>, ModificationOperation<Color>>
{
    protected override Color Lerp(Color a, Color b, float weight)
    {
        return a.Lerp(b, weight);
    }

    ModificationOperation<Color> IModifiable<Mod.Add<Color>, ModificationOperation<Color>>.InToParam(Mod.Add<Color> value)
    {
        return new ModificationOperation<Color>(Functions.Add, value.Operand);
    }

    ModificationOperation<Color> IModifiable<Mod.Multiply<Color>, ModificationOperation<Color>>.InToParam(Mod.Multiply<Color> value)
    {
        return new ModificationOperation<Color>(Functions.Multiply, value.Operand);
    }

    ModificationOperation<Color> IModifiable<Mod.Blend<Color>, ModificationOperation<Color>>.InToParam(Mod.Blend<Color> value)
    {
        return new ModificationOperation<Color>(Functions.Blend, value.Operand);
    }

    ModificationOperation<Color> IModifiable<Mod.Add<Variant>, ModificationOperation<Color>>.InToParam(Mod.Add<Variant> value)
    {
        return new ModificationOperation<Color>(Functions.Add, value.Operand.AsColor());
    }

    ModificationOperation<Color> IModifiable<Mod.Multiply<Variant>, ModificationOperation<Color>>.InToParam(Mod.Multiply<Variant> value)
    {
        return new ModificationOperation<Color>(Functions.Multiply, value.Operand.AsColor());
    }

    ModificationOperation<Color> IModifiable<Mod.Blend<Variant>, ModificationOperation<Color>>.InToParam(Mod.Blend<Variant> value)
    {
        return new ModificationOperation<Color>(Functions.Blend, value.Operand.AsColor());
    }
    
    public class Functions
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

public partial class Vector2Attribute :
    StandardAttribute<Vector2, Vector2ModifierChain>,
    IModifiable<Mod.Add<Vector2>>,
    IModifiable<Mod.Multiply<Vector2>>,
    IModifiable<Mod.Add<Variant>>,
    IModifiable<Mod.Multiply<Variant>>
{
    public Vector2Attribute() {}
    public Vector2Attribute(Vector2 baseValue) : base(baseValue) {}
    
    public abstract class Functions : Vector2ModifierChain.Functions;

    public void Modify(ModifierToken token, Mod.Add<Vector2> value)
    {
        GetModifierChain().Modify(token, value);
    }

    public void Modify(ModifierToken token, Mod.Multiply<Vector2> value)
    {
        GetModifierChain().Modify(token, value);
    }

    public void Modify(ModifierToken token, Mod.Add<Variant> value)
    {
        GetModifierChain().Modify(token, value);
    }

    public void Modify(ModifierToken token, Mod.Multiply<Variant> value)
    {
        GetModifierChain().Modify(token, value);
    }
}
public class Vector2ModifierChain: StandardModifierChain<Vector2>,
    ITypedStandardModifierChain<Vector2>,
    IVariantStandardModifierChain<Vector2>,
    IModifiable<Mod.Add<Vector2>, ModificationOperation<Vector2>>,
    IModifiable<Mod.Multiply<Vector2>, ModificationOperation<Vector2>>,
    IModifiable<Mod.Add<Variant>, ModificationOperation<Vector2>>,
    IModifiable<Mod.Multiply<Variant>, ModificationOperation<Vector2>>
{
    protected override Vector2 Lerp(Vector2 a, Vector2 b, float weight)
    {
        return a.Lerp(b, weight);
    }

    ModificationOperation<Vector2> IModifiable<Mod.Add<Vector2>, ModificationOperation<Vector2>>.InToParam(Mod.Add<Vector2> value)
    {
        return new ModificationOperation<Vector2>(Functions.Add, value.Operand);
    }

    ModificationOperation<Vector2> IModifiable<Mod.Multiply<Vector2>, ModificationOperation<Vector2>>.InToParam(Mod.Multiply<Vector2> value)
    {
        return new ModificationOperation<Vector2>(Functions.Multiply, value.Operand);
    }

    ModificationOperation<Vector2> IModifiable<Mod.Add<Variant>, ModificationOperation<Vector2>>.InToParam(Mod.Add<Variant> value)
    {
        return new ModificationOperation<Vector2>(Functions.Add, value.Operand.AsVector2());
    }

    ModificationOperation<Vector2> IModifiable<Mod.Multiply<Variant>, ModificationOperation<Vector2>>.InToParam(Mod.Multiply<Variant> value)
    {
        return new ModificationOperation<Vector2>(Functions.Multiply, value.Operand.AsVector2());
    }
    
    public class Functions
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

public partial class Vector3Attribute :
    StandardAttribute<Vector3, Vector3ModifierChain>,
    IModifiable<Mod.Add<Vector3>>,
    IModifiable<Mod.Multiply<Vector3>>,
    IModifiable<Mod.Add<Variant>>,
    IModifiable<Mod.Multiply<Variant>>
{
    public Vector3Attribute() {}
    public Vector3Attribute(Vector3 baseValue) : base(baseValue) {}
    
    public abstract class Functions : Vector3ModifierChain.Functions;

    public void Modify(ModifierToken token, Mod.Add<Vector3> value)
    {
        GetModifierChain().Modify(token, value);
    }

    public void Modify(ModifierToken token, Mod.Multiply<Vector3> value)
    {
        GetModifierChain().Modify(token, value);
    }

    public void Modify(ModifierToken token, Mod.Add<Variant> value)
    {
        GetModifierChain().Modify(token, value);
    }

    public void Modify(ModifierToken token, Mod.Multiply<Variant> value)
    {
        GetModifierChain().Modify(token, value);
    }
}
public class Vector3ModifierChain: StandardModifierChain<Vector3>,
    ITypedStandardModifierChain<Vector3>,
    IVariantStandardModifierChain<Vector3>,
    IModifiable<Mod.Add<Vector3>, ModificationOperation<Vector3>>,
    IModifiable<Mod.Multiply<Vector3>, ModificationOperation<Vector3>>,
    IModifiable<Mod.Add<Variant>, ModificationOperation<Vector3>>,
    IModifiable<Mod.Multiply<Variant>, ModificationOperation<Vector3>>
{
    protected override Vector3 Lerp(Vector3 a, Vector3 b, float weight)
    {
        return a.Lerp(b, weight);
    }

    ModificationOperation<Vector3> IModifiable<Mod.Add<Vector3>, ModificationOperation<Vector3>>.InToParam(Mod.Add<Vector3> value)
    {
        return new ModificationOperation<Vector3>(Functions.Add, value.Operand);
    }

    ModificationOperation<Vector3> IModifiable<Mod.Multiply<Vector3>, ModificationOperation<Vector3>>.InToParam(Mod.Multiply<Vector3> value)
    {
        return new ModificationOperation<Vector3>(Functions.Multiply, value.Operand);
    }

    ModificationOperation<Vector3> IModifiable<Mod.Add<Variant>, ModificationOperation<Vector3>>.InToParam(Mod.Add<Variant> value)
    {
        return new ModificationOperation<Vector3>(Functions.Add, value.Operand.AsVector3());
    }

    ModificationOperation<Vector3> IModifiable<Mod.Multiply<Variant>, ModificationOperation<Vector3>>.InToParam(Mod.Multiply<Variant> value)
    {
        return new ModificationOperation<Vector3>(Functions.Multiply, value.Operand.AsVector3());
    }
    
    public class Functions
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

public partial class Vector4Attribute :
    StandardAttribute<Vector4, Vector4ModifierChain>,
    IModifiable<Mod.Add<Vector4>>,
    IModifiable<Mod.Multiply<Vector4>>,
    IModifiable<Mod.Add<Variant>>,
    IModifiable<Mod.Multiply<Variant>>
{
    public Vector4Attribute() {}
    public Vector4Attribute(Vector4 baseValue) : base(baseValue) {}
    
    public abstract class Functions : Vector4ModifierChain.Functions;

    public void Modify(ModifierToken token, Mod.Add<Vector4> value)
    {
        GetModifierChain().Modify(token, value);
    }

    public void Modify(ModifierToken token, Mod.Multiply<Vector4> value)
    {
        GetModifierChain().Modify(token, value);
    }

    public void Modify(ModifierToken token, Mod.Add<Variant> value)
    {
        GetModifierChain().Modify(token, value);
    }

    public void Modify(ModifierToken token, Mod.Multiply<Variant> value)
    {
        GetModifierChain().Modify(token, value);
    }
}
public class Vector4ModifierChain: StandardModifierChain<Vector4>,
    ITypedStandardModifierChain<Vector4>,
    IVariantStandardModifierChain<Vector4>,
    IModifiable<Mod.Add<Vector4>, ModificationOperation<Vector4>>,
    IModifiable<Mod.Multiply<Vector4>, ModificationOperation<Vector4>>,
    IModifiable<Mod.Add<Variant>, ModificationOperation<Vector4>>,
    IModifiable<Mod.Multiply<Variant>, ModificationOperation<Vector4>>
{
    protected override Vector4 Lerp(Vector4 a, Vector4 b, float weight)
    {
        return a.Lerp(b, weight);
    }

    ModificationOperation<Vector4> IModifiable<Mod.Add<Vector4>, ModificationOperation<Vector4>>.InToParam(Mod.Add<Vector4> value)
    {
        return new ModificationOperation<Vector4>(Functions.Add, value.Operand);
    }

    ModificationOperation<Vector4> IModifiable<Mod.Multiply<Vector4>, ModificationOperation<Vector4>>.InToParam(Mod.Multiply<Vector4> value)
    {
        return new ModificationOperation<Vector4>(Functions.Multiply, value.Operand);
    }

    ModificationOperation<Vector4> IModifiable<Mod.Add<Variant>, ModificationOperation<Vector4>>.InToParam(Mod.Add<Variant> value)
    {
        return new ModificationOperation<Vector4>(Functions.Add, value.Operand.AsVector4());
    }

    ModificationOperation<Vector4> IModifiable<Mod.Multiply<Variant>, ModificationOperation<Vector4>>.InToParam(Mod.Multiply<Variant> value)
    {
        return new ModificationOperation<Vector4>(Functions.Multiply, value.Operand.AsVector4());
    }
    
    public class Functions
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

public partial class QuaternionAttribute :
    StandardAttribute<Quaternion, QuaternionModifierChain>,
    IModifiable<Mod.Add<Quaternion>>,
    IModifiable<Mod.Multiply<Quaternion>>,
    IModifiable<Mod.Add<Variant>>,
    IModifiable<Mod.Multiply<Variant>>
{
    public QuaternionAttribute() : base(Quaternion.Identity) {}
    public QuaternionAttribute(Quaternion baseValue) : base(baseValue) {}
    
    public abstract class Functions : QuaternionModifierChain.Functions;

    public void Modify(ModifierToken token, Mod.Add<Quaternion> value)
    {
        GetModifierChain().Modify(token, value);
    }

    public void Modify(ModifierToken token, Mod.Multiply<Quaternion> value)
    {
        GetModifierChain().Modify(token, value);
    }

    public void Modify(ModifierToken token, Mod.Add<Variant> value)
    {
        GetModifierChain().Modify(token, value);
    }

    public void Modify(ModifierToken token, Mod.Multiply<Variant> value)
    {
        GetModifierChain().Modify(token, value);
    }
}
public class QuaternionModifierChain: StandardModifierChain<Quaternion>,
    ITypedStandardModifierChain<Quaternion>,
    IVariantStandardModifierChain<Quaternion>,
    IModifiable<Mod.Add<Quaternion>, ModificationOperation<Quaternion>>,
    IModifiable<Mod.Multiply<Quaternion>, ModificationOperation<Quaternion>>,
    IModifiable<Mod.Add<Variant>, ModificationOperation<Quaternion>>,
    IModifiable<Mod.Multiply<Variant>, ModificationOperation<Quaternion>>
{
    protected override Quaternion Lerp(Quaternion a, Quaternion b, float weight)
    {
        return a.Slerp(b, weight);
    }

    ModificationOperation<Quaternion> IModifiable<Mod.Add<Quaternion>, ModificationOperation<Quaternion>>.InToParam(Mod.Add<Quaternion> value)
    {
        return new ModificationOperation<Quaternion>(Functions.Add, value.Operand);
    }

    ModificationOperation<Quaternion> IModifiable<Mod.Multiply<Quaternion>, ModificationOperation<Quaternion>>.InToParam(Mod.Multiply<Quaternion> value)
    {
        return new ModificationOperation<Quaternion>(Functions.Multiply, value.Operand);
    }

    ModificationOperation<Quaternion> IModifiable<Mod.Add<Variant>, ModificationOperation<Quaternion>>.InToParam(Mod.Add<Variant> value)
    {
        return new ModificationOperation<Quaternion>(Functions.Add, value.Operand.AsQuaternion());
    }

    ModificationOperation<Quaternion> IModifiable<Mod.Multiply<Variant>, ModificationOperation<Quaternion>>.InToParam(Mod.Multiply<Variant> value)
    {
        return new ModificationOperation<Quaternion>(Functions.Multiply, value.Operand.AsQuaternion());
    }
    
    public class Functions
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
