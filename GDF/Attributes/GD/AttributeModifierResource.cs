using GDF.Attributes.Impl;
using Godot;
using Godot.Collections;
using Array = System.Array;

namespace GDF.Attributes;

[Tool]
[GlobalClass]
public partial class AttributeModifierResource : Resource, IModifyInvoker
{
    protected static readonly StringName PropertyNameOperand = "Operand";
    
    [Export]
    public Variant.Type Type
    {
        get => _type;
        set
        {
            if (_type == value) return;
            _type = value;
            _operand = GetDefaultForType(Type);
        }
    }

    [Export] private Variant _operand;
    [Export(PropertyHint.EnumSuggestion, "Set,Add,Multiply,And,Or,Blend")]
    public string Function = "Set";

    public Variant Operand
    {
        get => _operand;
        set => _operand = value;
    }
    
    private Variant.Type _type = Variant.Type.Nil;

    public AttributeModifierResource()
    {
        ResourceLocalToScene = true;
    }

    public void Modify(ModifierToken token)
    {
        if (!TryModify(token))
        {
            GdfLogger.Error($"Failed to modify attribute using AttributeModifierResource; The combination of type '{Type}' and function '{Function}' is not supported by ModifierChain type '{token.Target.GetType()}'");
        }
    }

    public bool TryModify(ModifierToken token)
    {
        var operandType = Type != Variant.Type.Nil ? Type : _operand.VariantType;
        return operandType switch
        {
            Variant.Type.Nil => false,
            Variant.Type.Bool => ModifyTyped<bool>(token, _operand),
            Variant.Type.Int => ModifyTyped<int>(token, _operand),
            Variant.Type.Float => ModifyTyped<float>(token, _operand),
            Variant.Type.String => ModifyTyped<string>(token, _operand),
            Variant.Type.Vector2 => ModifyTyped<Vector2>(token, _operand),
            Variant.Type.Vector2I => ModifyTyped<Vector2I>(token, _operand),
            Variant.Type.Rect2 => ModifyTyped<Rect2>(token, _operand),
            Variant.Type.Rect2I => ModifyTyped<Rect2I>(token, _operand),
            Variant.Type.Vector3 => ModifyTyped<Vector3>(token, _operand),
            Variant.Type.Vector3I => ModifyTyped<Vector3I>(token, _operand),
            Variant.Type.Transform2D => ModifyTyped<Transform2D>(token, _operand),
            Variant.Type.Vector4 => ModifyTyped<Vector4>(token, _operand),
            Variant.Type.Vector4I => ModifyTyped<Vector4I>(token, _operand),
            Variant.Type.Plane => ModifyTyped<Plane>(token, _operand),
            Variant.Type.Quaternion => ModifyTyped<Quaternion>(token, _operand),
            Variant.Type.Aabb => ModifyTyped<Aabb>(token, _operand),
            Variant.Type.Basis => ModifyTyped<Basis>(token, _operand),
            Variant.Type.Transform3D => ModifyTyped<Transform3D>(token, _operand),
            Variant.Type.Projection => ModifyTyped<Projection>(token, _operand),
            Variant.Type.Color => ModifyTyped<Color>(token, _operand),
            Variant.Type.StringName => ModifyTyped<StringName>(token, _operand),
            Variant.Type.NodePath => ModifyTyped<NodePath>(token, _operand),
            Variant.Type.Rid => ModifyTyped<Rid>(token, _operand),
            Variant.Type.Object => ModifyTyped<GodotObject>(token, _operand),
            Variant.Type.Callable => ModifyTyped<Callable>(token, _operand),
            Variant.Type.Signal => ModifyTyped<Signal>(token, _operand),
            Variant.Type.Dictionary => ModifyTyped<Godot.Collections.Dictionary>(token, _operand),
            Variant.Type.Array => ModifyTyped<Godot.Collections.Array>(token, _operand),
            Variant.Type.PackedByteArray => ModifyTyped<byte[]>(token, _operand),
            Variant.Type.PackedInt32Array => ModifyTyped<int[]>(token, _operand),
            Variant.Type.PackedInt64Array => ModifyTyped<long[]>(token, _operand),
            Variant.Type.PackedFloat32Array => ModifyTyped<float[]>(token, _operand),
            Variant.Type.PackedFloat64Array => ModifyTyped<double[]>(token, _operand),
            Variant.Type.PackedStringArray => ModifyTyped<string[]>(token, _operand),
            Variant.Type.PackedVector2Array => ModifyTyped<Vector2[]>(token, _operand),
            Variant.Type.PackedVector3Array => ModifyTyped<Vector3[]>(token, _operand),
            Variant.Type.PackedColorArray => ModifyTyped<Color[]>(token, _operand),
            Variant.Type.PackedVector4Array => ModifyTyped<Vector4[]>(token, _operand),
            _ => false
        };
    }

    private bool ModifyTyped<[MustBeVariant] T>(ModifierToken token, Variant operandVariant)
    {
        var functionName = Function;
        var operand = operandVariant.As<T>();
        switch (functionName)
        {
            case "Set":
            {
                if (token.Target is IModifiable<Mod.Set<T>> modifiable)
                {
                    modifiable.Modify(token, new Mod.Set<T>(operand));
                    return true;
                }
                return false;
            }
            case "Add":
            {
                if (token.Target is IModifiable<Mod.Add<T>> modifiable)
                {
                    modifiable.Modify(token, new Mod.Add<T>(operand));
                    return true;
                }
                return false;
            }
            case "Multiply":
            {
                if (token.Target is IModifiable<Mod.Multiply<T>> modifiable)
                {
                    modifiable.Modify(token, new Mod.Multiply<T>(operand));
                    return true;
                }
                return false;
            }
            case "And":
            {
                if (token.Target is IModifiable<Mod.And<T>> modifiable)
                {
                    modifiable.Modify(token, new Mod.And<T>(operand));
                    return true;
                }
                return false;
            }
            case "Or":
            {
                if (token.Target is IModifiable<Mod.Or<T>> modifiable)
                {
                    modifiable.Modify(token, new Mod.Or<T>(operand));
                    return true;
                }
                return false;
            }
            case "Blend":
            {
                if (token.Target is IModifiable<Mod.Blend<T>> modifiable)
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

    public override Array<Dictionary> _GetPropertyList()
    {
        return new Array<Dictionary>
        {
            new()
            {
                { "name", PropertyNameOperand },
                { "type", Variant.From(Type) },
                { "hint", Variant.From(PropertyHint.None) },
                { "hint_string", "" },
                { "usage", Variant.From(PropertyUsageFlags.Editor) }
            }
        };
    }

    public override Variant _Get(StringName property)
    {
        if (property == PropertyNameOperand)
        {
            return _operand;
        }
        return base._Get(property);
    }

    public override bool _Set(StringName property, Variant value)
    {
        if (property == PropertyNameOperand)
        {
            _operand = value;
            return true;
        }
        return base._Set(property, value);
    }

    public override bool _PropertyCanRevert(StringName property)
    {
        if (property == PropertyNameOperand)
        {
            return true;
        }
        return base._PropertyCanRevert(property);
    }

    public override Variant _PropertyGetRevert(StringName property)
    {
        if (property == PropertyNameOperand)
        {
            return GetDefaultForType(Type);
        }
        return base._PropertyGetRevert(property);
    }

    private static Variant GetDefaultForType(Variant.Type type)
    {
        return type switch
        {
            Variant.Type.Nil => default,
            Variant.Type.Bool => false,
            Variant.Type.Int => 0,
            Variant.Type.Float => 0f,
            Variant.Type.String => "",
            Variant.Type.Vector2 => Vector2.Zero,
            Variant.Type.Vector2I => Vector2I.Zero,
            Variant.Type.Rect2 => new Rect2(),
            Variant.Type.Rect2I => new Rect2I(),
            Variant.Type.Vector3 => Vector3.Zero,
            Variant.Type.Vector3I => Vector3I.Zero,
            Variant.Type.Transform2D => Transform2D.Identity,
            Variant.Type.Vector4 => Vector4.Zero,
            Variant.Type.Vector4I => Vector4I.Zero,
            Variant.Type.Plane => new Plane(),
            Variant.Type.Quaternion => Quaternion.Identity,
            Variant.Type.Aabb => new Aabb(),
            Variant.Type.Basis => Basis.Identity,
            Variant.Type.Transform3D => Transform3D.Identity,
            Variant.Type.Projection => Projection.Identity,
            Variant.Type.Color => Colors.White,
            Variant.Type.StringName => new StringName(""),
            Variant.Type.NodePath => new NodePath(),
            Variant.Type.Rid => new Rid(),
            Variant.Type.Object => (GodotObject)null,
            Variant.Type.Callable => new Callable(),
            Variant.Type.Signal => new Signal(),
            Variant.Type.Dictionary => new Godot.Collections.Dictionary(),
            Variant.Type.Array => new Godot.Collections.Array(),
            Variant.Type.PackedByteArray => Array.Empty<byte>(),
            Variant.Type.PackedInt32Array => Array.Empty<int>(),
            Variant.Type.PackedInt64Array => Array.Empty<long>(),
            Variant.Type.PackedFloat32Array => Array.Empty<float>(),
            Variant.Type.PackedFloat64Array => Array.Empty<double>(),
            Variant.Type.PackedStringArray => Array.Empty<string>(),
            Variant.Type.PackedVector2Array => Array.Empty<Vector2>(),
            Variant.Type.PackedVector3Array => Array.Empty<Vector3>(),
            Variant.Type.PackedColorArray => Array.Empty<Color>(),
            Variant.Type.PackedVector4Array => Array.Empty<Vector4>(),
            _ => default
        };
    }

    public override void _ValidateProperty(Dictionary property)
    {
        base._ValidateProperty(property);
        var propName = property["name"].AsStringName();
        var usage = property["usage"].As<PropertyUsageFlags>();

        if (propName == PropertyName.Type)
        {
            usage |= PropertyUsageFlags.UpdateAllIfModified;
        }
        else if (propName == PropertyNameOperand)
        {
            property["type"] = Variant.From(Type);
        }
        else if (propName == PropertyName._operand)
        {
            usage &= ~PropertyUsageFlags.Editor;
        }

        property["usage"] = Variant.From(usage);
    }
}