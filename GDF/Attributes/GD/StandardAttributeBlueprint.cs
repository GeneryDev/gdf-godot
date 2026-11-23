using GDF.Attributes.Impl;
using GDF.Util;
using Godot;
using Godot.Collections;

namespace GDF.Attributes;

[Tool]
[GlobalClass]
public partial class StandardAttributeBlueprint : AttributeBlueprint
{
    private static readonly StringName PropertyNameBaseValue = "BaseValue";
    
    [Export]
    public StringName Id = "";

    [Export]
    public TypeEnum Type
    {
        get => _type;
        set
        {
            if (_type == value) return;
            _type = value;
            _baseValue = GetDefaultForType(Type);
        }
    }

    [Export] private Variant _baseValue;
    [Export(PropertyHint.EnumSuggestion,"Set")] public string DefaultFunction = "Set";

    [Export(PropertyHint.Enum,"Linear,Radians,Degrees")]
    public int FloatInterpolationMode = FloatModifierChain.InterpolationModeLinear;
    
    private TypeEnum _type = TypeEnum.Float;

    public override void CreateAttributes(System.Collections.Generic.Dictionary<StringName, IAttribute> output)
    {
        if (Id.IsNullOrEmpty()) return;
        var attr = CreateAttribute();
        if (attr == null) return;
        output[Id] = attr;
    }

    private IAttribute CreateAttribute()
    {
        switch (Type)
        {
            case TypeEnum.Float:
                return new FloatAttribute(_baseValue.AsSingle())
                {
                    DefaultFunction = FloatAttribute.Functions.FromName(DefaultFunction),
                    InterpolationMode = FloatInterpolationMode
                };
            case TypeEnum.Bool:
                return new BoolAttribute(_baseValue.AsBool())
                {
                    DefaultFunction = BoolAttribute.Functions.FromName(DefaultFunction)
                };
            case TypeEnum.Int:
                return new IntAttribute(_baseValue.AsInt32())
                {
                    DefaultFunction = IntAttribute.Functions.FromName(DefaultFunction)
                };
            case TypeEnum.Color:
                return new ColorAttribute(_baseValue.AsColor())
                {
                    DefaultFunction = ColorAttribute.Functions.FromName(DefaultFunction)
                };
            case TypeEnum.Vector2:
                return new Vector2Attribute(_baseValue.AsVector2())
                {
                    DefaultFunction = Vector2Attribute.Functions.FromName(DefaultFunction)
                };
            case TypeEnum.Vector3:
                return new Vector3Attribute(_baseValue.AsVector3())
                {
                    DefaultFunction = Vector3Attribute.Functions.FromName(DefaultFunction)
                };
            case TypeEnum.Vector4:
                return new Vector4Attribute(_baseValue.AsVector4())
                {
                    DefaultFunction = Vector4Attribute.Functions.FromName(DefaultFunction)
                };
            case TypeEnum.Quaternion:
                return new QuaternionAttribute(_baseValue.AsQuaternion())
                {
                    DefaultFunction = QuaternionAttribute.Functions.FromName(DefaultFunction)
                };
            default:
                return null;
        }
    }

    public override Array<Dictionary> _GetPropertyList()
    {
        return new Array<Dictionary>
        {
            new()
            {
                { "name", PropertyNameBaseValue },
                { "type", Variant.From(GetVariantType(Type)) },
                { "hint", Variant.From(PropertyHint.None) },
                { "hint_string", "" },
                { "usage", Variant.From(PropertyUsageFlags.Editor) }
            }
        };
    }

    public override Variant _Get(StringName property)
    {
        if (property == PropertyNameBaseValue)
        {
            return _baseValue;
        }
        return base._Get(property);
    }

    public override bool _Set(StringName property, Variant value)
    {
        if (property == PropertyNameBaseValue)
        {
            _baseValue = value;
            return true;
        }
        return base._Set(property, value);
    }

    public override bool _PropertyCanRevert(StringName property)
    {
        if (property == PropertyNameBaseValue)
        {
            return true;
        }
        return base._PropertyCanRevert(property);
    }

    public override Variant _PropertyGetRevert(StringName property)
    {
        if (property == PropertyNameBaseValue)
        {
            return GetDefaultForType(Type);
        }
        return base._PropertyGetRevert(property);
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
        else if (propName == PropertyNameBaseValue)
        {
            property["type"] = Variant.From(GetVariantType(Type));
        }
        else if (propName == PropertyName._baseValue)
        {
            usage &= ~PropertyUsageFlags.Editor;
        }
        else if (propName == PropertyName.FloatInterpolationMode)
        {
            if (Type != TypeEnum.Float)
                usage &= ~(PropertyUsageFlags.Editor | PropertyUsageFlags.Storage);
        }
        else if (propName == PropertyName.DefaultFunction)
        {
            switch (Type)
            {
                case TypeEnum.Float:
                    property["hint_string"] = "Set,Add,Multiply";
                    break;
                case TypeEnum.Bool:
                    property["hint_string"] = "Set,And,Or";
                    break;
                case TypeEnum.Int:
                    property["hint_string"] = "Set,Add,Multiply,And,Or";
                    break;
                case TypeEnum.Color:
                    property["hint_string"] = "Set,Add,Blend,Multiply";
                    break;
                case TypeEnum.Vector2:
                case TypeEnum.Vector3:
                case TypeEnum.Vector4:
                case TypeEnum.Quaternion:
                    property["hint_string"] = "Set,Add,Multiply";
                    break;
            }
        }

        property["usage"] = Variant.From(usage);
    }

    private static Variant GetDefaultForType(TypeEnum type)
    {
        return type switch
        {
            TypeEnum.Float => 0f,
            TypeEnum.Bool => false,
            TypeEnum.Int => 0,
            TypeEnum.Color => Colors.White,
            TypeEnum.Vector2 => Vector2.Zero,
            TypeEnum.Vector3 => Vector3.Zero,
            TypeEnum.Vector4 => Vector4.Zero,
            TypeEnum.Quaternion => Quaternion.Identity,
            _ => default
        };
    }

    private static Variant.Type GetVariantType(TypeEnum type)
    {
        return type switch
        {
            TypeEnum.Float => Variant.Type.Float,
            TypeEnum.Bool => Variant.Type.Bool,
            TypeEnum.Int => Variant.Type.Int,
            TypeEnum.Color => Variant.Type.Color,
            TypeEnum.Vector2 => Variant.Type.Vector2,
            TypeEnum.Vector3 => Variant.Type.Vector3,
            TypeEnum.Vector4 => Variant.Type.Vector4,
            TypeEnum.Quaternion => Variant.Type.Quaternion,
            _ => Variant.Type.Nil
        };
    }

    public enum TypeEnum
    {
        Float,
        Bool,
        Int,
        Color,
        Vector2,
        Vector3,
        Vector4,
        Quaternion
    }
}