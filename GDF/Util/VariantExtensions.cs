using System;
using Godot;

namespace GDF.Util;

public static class VariantExtensions
{
    private static bool AreTypesComparable(Variant.Type a, Variant.Type b)
    {
        if (a == b) return true;

        if (b == Variant.Type.String && a == Variant.Type.StringName) return true;
        if (a == Variant.Type.String && b == Variant.Type.StringName) return true;
        if (b == Variant.Type.String && a == Variant.Type.NodePath) return true;
        if (a == Variant.Type.String && b == Variant.Type.NodePath) return true;

        return false;
    }
    
    // Function by waldnercharles on Github, with a fix to the Callable branch
    // https://github.com/godotengine/godot-proposals/issues/5758#issuecomment-1603407047
    public static bool VariantEquals(this Variant left, Variant right)
    {
        if (left.VariantType != right.VariantType)
        {
            return false;
        }

        return left.VariantType switch
        {
            Variant.Type.Nil => true,
            Variant.Type.Bool => left.AsBool().Equals(right.AsBool()),
            Variant.Type.Int => left.AsInt64().Equals(right.AsInt64()),
            Variant.Type.Float => left.AsDouble().Equals(right.AsDouble()),
            Variant.Type.String => left.AsString().Equals(right.AsString()),
            Variant.Type.Vector2 => left.AsVector2().Equals(right.AsVector2()),
            Variant.Type.Vector2I => left.AsVector2I().Equals(right.AsVector2I()),
            Variant.Type.Rect2 => left.AsRect2().Equals(right.AsRect2()),
            Variant.Type.Rect2I => left.AsRect2I().Equals(right.AsRect2I()),
            Variant.Type.Vector3 => left.AsVector3().Equals(right.AsVector3()),
            Variant.Type.Vector3I => left.AsVector3I().Equals(right.AsVector3I()),
            Variant.Type.Transform2D => left.AsTransform2D().Equals(right.AsTransform2D()),
            Variant.Type.Vector4 => left.AsVector4().Equals(right.AsVector4()),
            Variant.Type.Vector4I => left.AsVector4I().Equals(right.AsVector4I()),
            Variant.Type.Plane => left.AsPlane().Equals(right.AsPlane()),
            Variant.Type.Quaternion => left.AsQuaternion().Equals(right.AsQuaternion()),
            Variant.Type.Aabb => left.AsAabb().Equals(right.AsAabb()),
            Variant.Type.Basis => left.AsBasis().Equals(right.AsBasis()),
            Variant.Type.Transform3D => left.AsTransform3D().Equals(right.AsTransform3D()),
            Variant.Type.Projection => left.AsProjection().Equals(right.AsProjection()),
            Variant.Type.Color => left.AsColor().Equals(right.AsColor()),
            Variant.Type.StringName => left.AsStringName().Equals(right.AsStringName()),
            Variant.Type.NodePath => left.AsNodePath().Equals(right.AsNodePath()),
            Variant.Type.Rid => left.AsRid().Equals(right.AsRid()),
            Variant.Type.Object => left.AsGodotObject().Equals(right.AsGodotObject()),
            Variant.Type.Callable => left.AsCallable().Equals(right.AsCallable()),
            Variant.Type.Signal => left.AsSignal().Equals(right.AsSignal()),
            Variant.Type.Dictionary => left.AsGodotDictionary().Equals(right.AsGodotDictionary()),
            Variant.Type.Array => left.AsGodotArray().Equals(right.AsGodotArray()),
            Variant.Type.PackedByteArray => left.AsByteArray().Equals(right.AsByteArray()),
            Variant.Type.PackedInt32Array => left.AsInt32Array().Equals(right.AsInt32Array()),
            Variant.Type.PackedInt64Array => left.AsInt64Array().Equals(right.AsInt64Array()),
            Variant.Type.PackedFloat32Array => left.AsFloat32Array().Equals(right.AsFloat32Array()),
            Variant.Type.PackedFloat64Array => left.AsFloat64Array().Equals(right.AsFloat64Array()),
            Variant.Type.PackedStringArray => left.AsStringArray().Equals(right.AsStringArray()),
            Variant.Type.PackedVector2Array => left.AsVector2Array().Equals(right.AsVector2Array()),
            Variant.Type.PackedVector3Array => left.AsVector3Array().Equals(right.AsVector3Array()),
            Variant.Type.PackedColorArray => left.AsColorArray().Equals(right.AsColorArray()),
            _ => throw new ArgumentOutOfRangeException(nameof(left))
        };
    }

    public static bool IsNullOrEmpty(this StringName name)
    {
        return name is not { IsEmpty: false };
    }

    public static bool IsNullOrEmpty(this NodePath name)
    {
        return name is not { IsEmpty: false };
    }

    public static Godot.Collections.Dictionary<TKey, TValue> StronglyTypeDictionary<[MustBeVariant] TKey,
        [MustBeVariant] TValue>(this Godot.Collections.Dictionary<TKey, TValue> inputDict)
    {
        if (inputDict == null) return null;
        var outputDict = new Godot.Collections.Dictionary<TKey, TValue>();
        foreach (var (key, value) in inputDict)
        {
            outputDict[key] = value;
        }
        return outputDict;
    }
}