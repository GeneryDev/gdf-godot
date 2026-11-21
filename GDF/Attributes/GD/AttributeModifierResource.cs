using GDF.Attributes.Impl;
using Godot;
using Godot.Collections;

namespace GDF.Attributes;

[Tool]
[GlobalClass]
public partial class AttributeModifierResource : Resource
{
    [Export] public Variant.Type OperandType;
    [Export(PropertyHint.EnumSuggestion, "Set,Add,Multiply,And,Or,Blend")]
    public string Function = "Set";
    [Export] public Variant Operand;

    public AttributeModifierResource()
    {
        ResourceLocalToScene = true;
    }

    public bool Modify(AttributeModifierToken token)
    {
        var operandType = OperandType != Variant.Type.Nil ? OperandType : Operand.VariantType;
        return operandType switch
        {
            Variant.Type.Nil => false,
            Variant.Type.Bool => ModifyTyped<bool>(token, Operand),
            Variant.Type.Int => ModifyTyped<int>(token, Operand),
            Variant.Type.Float => ModifyTyped<float>(token, Operand),
            Variant.Type.String => ModifyTyped<string>(token, Operand),
            Variant.Type.Vector2 => ModifyTyped<Vector2>(token, Operand),
            Variant.Type.Vector2I => ModifyTyped<Vector2I>(token, Operand),
            Variant.Type.Rect2 => ModifyTyped<Rect2>(token, Operand),
            Variant.Type.Rect2I => ModifyTyped<Rect2I>(token, Operand),
            Variant.Type.Vector3 => ModifyTyped<Vector3>(token, Operand),
            Variant.Type.Vector3I => ModifyTyped<Vector3I>(token, Operand),
            Variant.Type.Transform2D => ModifyTyped<Transform2D>(token, Operand),
            Variant.Type.Vector4 => ModifyTyped<Vector4>(token, Operand),
            Variant.Type.Vector4I => ModifyTyped<Vector4I>(token, Operand),
            Variant.Type.Plane => ModifyTyped<Plane>(token, Operand),
            Variant.Type.Quaternion => ModifyTyped<Quaternion>(token, Operand),
            Variant.Type.Aabb => ModifyTyped<Aabb>(token, Operand),
            Variant.Type.Basis => ModifyTyped<Basis>(token, Operand),
            Variant.Type.Transform3D => ModifyTyped<Transform3D>(token, Operand),
            Variant.Type.Projection => ModifyTyped<Projection>(token, Operand),
            Variant.Type.Color => ModifyTyped<Color>(token, Operand),
            Variant.Type.StringName => ModifyTyped<StringName>(token, Operand),
            Variant.Type.NodePath => ModifyTyped<NodePath>(token, Operand),
            Variant.Type.Rid => ModifyTyped<Rid>(token, Operand),
            Variant.Type.Object => ModifyTyped<GodotObject>(token, Operand),
            Variant.Type.Callable => ModifyTyped<Callable>(token, Operand),
            Variant.Type.Signal => ModifyTyped<Signal>(token, Operand),
            Variant.Type.Dictionary => ModifyTyped<Godot.Collections.Dictionary>(token, Operand),
            Variant.Type.Array => ModifyTyped<Godot.Collections.Array>(token, Operand),
            Variant.Type.PackedByteArray => ModifyTyped<byte[]>(token, Operand),
            Variant.Type.PackedInt32Array => ModifyTyped<int[]>(token, Operand),
            Variant.Type.PackedInt64Array => ModifyTyped<long[]>(token, Operand),
            Variant.Type.PackedFloat32Array => ModifyTyped<float[]>(token, Operand),
            Variant.Type.PackedFloat64Array => ModifyTyped<double[]>(token, Operand),
            Variant.Type.PackedStringArray => ModifyTyped<string[]>(token, Operand),
            Variant.Type.PackedVector2Array => ModifyTyped<Vector2[]>(token, Operand),
            Variant.Type.PackedVector3Array => ModifyTyped<Vector3[]>(token, Operand),
            Variant.Type.PackedColorArray => ModifyTyped<Color[]>(token, Operand),
            Variant.Type.PackedVector4Array => ModifyTyped<Vector4[]>(token, Operand),
            _ => false
        };
    }

    private bool ModifyTyped<[MustBeVariant] T>(AttributeModifierToken token, Variant operandVariant)
    {
        var functionName = Function;
        var operand = operandVariant.As<T>();
        switch (functionName)
        {
            case "Set":
            {
                if (token.Target is IAttributeModifiable<Mod.Set<T>> modifiable)
                {
                    modifiable.Modify(token, new Mod.Set<T>(operand));
                    return true;
                }
                return false;
            }
            case "Add":
            {
                if (token.Target is IAttributeModifiable<Mod.Add<T>> modifiable)
                {
                    modifiable.Modify(token, new Mod.Add<T>(operand));
                    return true;
                }
                return false;
            }
            case "Multiply":
            {
                if (token.Target is IAttributeModifiable<Mod.Multiply<T>> modifiable)
                {
                    modifiable.Modify(token, new Mod.Multiply<T>(operand));
                    return true;
                }
                return false;
            }
            case "And":
            {
                if (token.Target is IAttributeModifiable<Mod.And<T>> modifiable)
                {
                    modifiable.Modify(token, new Mod.And<T>(operand));
                    return true;
                }
                return false;
            }
            case "Or":
            {
                if (token.Target is IAttributeModifiable<Mod.Or<T>> modifiable)
                {
                    modifiable.Modify(token, new Mod.Or<T>(operand));
                    return true;
                }
                return false;
            }
            case "Blend":
            {
                if (token.Target is IAttributeModifiable<Mod.Blend<T>> modifiable)
                {
                    modifiable.Modify(token, new Mod.Blend<T>(operand));
                    return true;
                }
                return false;
            }
            default:
                break;
        }

        return false;
    }

    public override void _ValidateProperty(Dictionary property)
    {
        base._ValidateProperty(property);
        var propName = property["name"].AsStringName();
        var usage = property["usage"].As<PropertyUsageFlags>();

        if (propName == PropertyName.OperandType)
        {
            usage |= PropertyUsageFlags.UpdateAllIfModified;
        }
        else if (propName == PropertyName.Operand)
        {
            property["type"] = Variant.From(OperandType);
        }

        property["usage"] = Variant.From(usage);
    }
}