using System;
using System.Globalization;
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
    
    public static string VariantToString(Variant v)
    {
#if GODOT4_7_1_OR_GREATER
// Variant.ToString and Variant.AsString is broken in this build, so add a hacky fix to convert values
        return v.VariantType switch
        {
            Variant.Type.Nil => "",
            Variant.Type.Bool => v.AsBool().ToString(),
            Variant.Type.Int => v.AsInt64().ToString(),
            Variant.Type.Float => v.AsDouble().ToString(CultureInfo.InvariantCulture),
            Variant.Type.String => v.AsString(),
            Variant.Type.Vector2 => v.AsVector2().ToString(),
            Variant.Type.Vector2I => v.AsVector2I().ToString(),
            Variant.Type.Rect2 => v.AsRect2().ToString(),
            Variant.Type.Rect2I => v.AsRect2I().ToString(),
            Variant.Type.Vector3 => v.AsVector3().ToString(),
            Variant.Type.Vector3I => v.AsVector3I().ToString(),
            Variant.Type.Transform2D => v.AsTransform2D().ToString(),
            Variant.Type.Vector4 => v.AsVector4().ToString(),
            Variant.Type.Vector4I => v.AsVector4I().ToString(),
            Variant.Type.Plane => v.AsPlane().ToString(),
            Variant.Type.Quaternion => v.AsQuaternion().ToString(),
            Variant.Type.Aabb => v.AsAabb().ToString(),
            Variant.Type.Basis => v.AsBasis().ToString(),
            Variant.Type.Transform3D => v.AsTransform3D().ToString(),
            Variant.Type.Projection => v.AsProjection().ToString(),
            Variant.Type.Color => v.AsColor().ToString(),
            Variant.Type.StringName => v.AsStringName().ToString(),
            Variant.Type.NodePath => v.AsNodePath().ToString(),
            Variant.Type.Rid => v.AsRid().ToString(),
            Variant.Type.Object => v.AsGodotObject().ToString(),
            Variant.Type.Callable => v.AsCallable().ToString(),
            Variant.Type.Signal => v.AsSignal().ToString(),
            Variant.Type.Dictionary => v.AsGodotDictionary().ToString(),
            Variant.Type.Array => v.AsGodotArray().ToString(),
            Variant.Type.PackedByteArray => v.AsByteArray().ToString(),
            Variant.Type.PackedInt32Array => v.AsInt32Array().ToString(),
            Variant.Type.PackedInt64Array => v.AsInt64Array().ToString(),
            Variant.Type.PackedFloat32Array => v.AsFloat32Array().ToString(),
            Variant.Type.PackedFloat64Array => v.AsFloat64Array().ToString(),
            Variant.Type.PackedStringArray => v.AsStringArray().ToString(),
            Variant.Type.PackedVector2Array => v.AsVector2Array().ToString(),
            Variant.Type.PackedVector3Array => v.AsVector3Array().ToString(),
            Variant.Type.PackedColorArray => v.AsColorArray().ToString(),
            _ => v.Obj?.ToString() ?? ""
        };
#else
    return v.AsString();
#endif
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