using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using Godot.Collections;

namespace GDF.Serialization;

public partial struct JsonSerializer
{
    public static readonly JsonSerializer Default = new()
    {
        PropertyNameCaseConversionMode = CaseConversionModeEnum.UseSnakeCase,
        PropertyOmissionHandlingMode = PropertyOmissionHandlingModeEnum.UseTypeDefault,
        EnumSerializationMode = EnumSerializationModeEnum.SaveAsString,
        EnumDeserializationMode = EnumDeserializationModeEnum.Strict,
        EnumNameCaseConversionMode = CaseConversionModeEnum.UseSnakeCase
    };
    
    public CaseConversionModeEnum PropertyNameCaseConversionMode;
    public PropertyOmissionHandlingModeEnum PropertyOmissionHandlingMode;
    public EnumSerializationModeEnum EnumSerializationMode;
    public EnumDeserializationModeEnum EnumDeserializationMode;
    public CaseConversionModeEnum EnumNameCaseConversionMode;

    private string GetPropertyName(string targetName)
    {
        return ConvertCase(targetName, PropertyNameCaseConversionMode);
    }

    private static string ConvertCase(string source, CaseConversionModeEnum mode)
    {
        if (source == null) return null;
        
        switch (mode)
        {
            case CaseConversionModeEnum.Keep: return source;
            case CaseConversionModeEnum.UseSnakeCase: return source.ToSnakeCase();
        }

        return source;
    }
    
    public void DeserializeVariant<[MustBeVariant]T>(Godot.Collections.Dictionary dict, string fieldName, ref T field)
    {
        string propertyName = GetPropertyName(fieldName);
        if (dict?.TryGetValue(propertyName, out var v) ?? false) field = v.As<T>();
        else if(PropertyOmissionHandlingMode == PropertyOmissionHandlingModeEnum.UseTypeDefault) field = default;
    }
    public void DeserializeVariants<[MustBeVariant]T>(Godot.Collections.Dictionary dict, string fieldName, ref List<T> field)
    {
        string propertyName = GetPropertyName(fieldName);
        if (dict?.TryGetValue(propertyName, out var v) ?? false)
        {
            var list = field ?? new List<T>();
            list.Clear();
            foreach (var rawValue in v.AsGodotArray())
            {
                list.Add(rawValue.As<T>());
            }
            field = list;
        }
        else if(PropertyOmissionHandlingMode == PropertyOmissionHandlingModeEnum.UseTypeDefault) field = default;
    }
    public void DeserializeVariantNullable<[MustBeVariant]T>(Godot.Collections.Dictionary dict, string fieldName, ref T? field) where T : struct
    {
        string propertyName = GetPropertyName(fieldName);
        if (dict?.TryGetValue(propertyName, out var v) ?? false) field = v.As<T>();
        else if(PropertyOmissionHandlingMode == PropertyOmissionHandlingModeEnum.UseTypeDefault) field = default;
    }
    public void DeserializeEnum<[MustBeVariant]T>(Godot.Collections.Dictionary dict, string fieldName, ref T field) where T : struct, Enum
    {
        string propertyName = GetPropertyName(fieldName);
        if (dict?.TryGetValue(propertyName, out var v) ?? false)
        {
            bool tryString = EnumSerializationMode == EnumSerializationModeEnum.SaveAsString ||
                             EnumDeserializationMode == EnumDeserializationModeEnum.Lenient;
            bool tryInt = EnumSerializationMode == EnumSerializationModeEnum.SaveAsInt ||
                          EnumDeserializationMode == EnumDeserializationModeEnum.Lenient;

            if (tryString && v.VariantType is Variant.Type.String or Variant.Type.StringName)
            {
                string rawConstantName = v.AsString();
                foreach (var enumValue in Enum.GetValues<T>())
                {
                    string constantName = ConvertCase(Enum.GetName(enumValue), EnumNameCaseConversionMode);
                    if (constantName == rawConstantName)
                    {
                        field = enumValue;
                        return;
                    }
                }
                GD.PushWarning($"Failed to deserialize enum value '{rawConstantName}' for enum type {typeof(T).Name}; using default value");
            }

            if (tryInt && v.VariantType is Variant.Type.Int or Variant.Type.Float)
            {
                field = v.As<T>();
                return;
            }
            
            field = default;
        }
        else if(PropertyOmissionHandlingMode == PropertyOmissionHandlingModeEnum.UseTypeDefault) field = default;
    }
    public void DeserializeEnum<[MustBeVariant]T>(Godot.Collections.Dictionary dict, string fieldName, ref T? field) where T : struct, Enum
    {
        string propertyName = GetPropertyName(fieldName);
        if (dict?.TryGetValue(propertyName, out var v) ?? false)
        {
            bool tryString = EnumSerializationMode == EnumSerializationModeEnum.SaveAsString ||
                             EnumDeserializationMode == EnumDeserializationModeEnum.Lenient;
            bool tryInt = EnumSerializationMode == EnumSerializationModeEnum.SaveAsInt ||
                          EnumDeserializationMode == EnumDeserializationModeEnum.Lenient;

            if (tryString && v.VariantType is Variant.Type.String or Variant.Type.StringName)
            {
                string rawConstantName = v.AsString();
                foreach (var enumValue in Enum.GetValues<T>())
                {
                    string constantName = ConvertCase(Enum.GetName(enumValue), EnumNameCaseConversionMode);
                    if (constantName == rawConstantName)
                    {
                        field = enumValue;
                        return;
                    }
                }
                GD.PushWarning($"Failed to deserialize enum value '{rawConstantName}' for enum type {typeof(T).Name}; using default value");
            }

            if (tryInt && v.VariantType is Variant.Type.Int or Variant.Type.Float)
            {
                field = v.As<T>();
                return;
            }
            
            field = default;
        }
        else if(PropertyOmissionHandlingMode == PropertyOmissionHandlingModeEnum.UseTypeDefault) field = default;
    }
    public void Deserialize<T>(Godot.Collections.Dictionary dict, string fieldName, ref T field) where T : IJsonSerializable, new()
    {
        string propertyName = GetPropertyName(fieldName);
        if (dict?.TryGetValue(propertyName, out var v) ?? false)
        {
            var value = new T();
            value.Deserialize(v);
            field = value;
        }
        else if(PropertyOmissionHandlingMode == PropertyOmissionHandlingModeEnum.UseTypeDefault) field = default;
    }
    public void Deserialize<T>(Godot.Collections.Dictionary dict, string fieldName, ref T? field) where T : struct, IJsonSerializable
    {
        string propertyName = GetPropertyName(fieldName);
        if (dict?.TryGetValue(propertyName, out var v) ?? false)
        {
            var value = new T();
            value.Deserialize(v);
            field = value;
        }
        else if(PropertyOmissionHandlingMode == PropertyOmissionHandlingModeEnum.UseTypeDefault) field = default;
    }
    public void Deserialize<TValue, TPoly>(Godot.Collections.Dictionary dict, string fieldName, ref TValue field) where TPoly : struct, IPolymorphicJsonSerializer<TValue>
    {
        string propertyName = GetPropertyName(fieldName);
        if (dict?.TryGetValue(propertyName, out var v) ?? false)
        {
            var value = new TPoly();
            field = value.Deserialize(v);
        }
        else if(PropertyOmissionHandlingMode == PropertyOmissionHandlingModeEnum.UseTypeDefault) field = default;
    }
    public void Deserialize<T>(Godot.Collections.Dictionary dict, string fieldName, ref List<T> field) where T : IJsonSerializable, new()
    {
        string propertyName = GetPropertyName(fieldName);
        if (dict?.TryGetValue(propertyName, out var v) ?? false)
        {
            var list = field ?? new List<T>();
            list.Clear();
            foreach (var rawValue in v.AsGodotArray())
            {
                var value = new T();
                value.Deserialize(rawValue);
                list.Add(value);
            }
            field = list;
        }
        else if(PropertyOmissionHandlingMode == PropertyOmissionHandlingModeEnum.UseTypeDefault) field = default;
    }
    public void Deserialize<T>(Godot.Collections.Dictionary dict, string fieldName, ref T[] field) where T : IJsonSerializable, new()
    {
        string propertyName = GetPropertyName(fieldName);
        if (dict?.TryGetValue(propertyName, out var v) ?? false)
        {
            var list = new List<T>();
            list.Clear();
            foreach (var rawValue in v.AsGodotArray())
            {
                var value = new T();
                value.Deserialize(rawValue);
                list.Add(value);
            }
            field = list.ToArray();
        }
        else if(PropertyOmissionHandlingMode == PropertyOmissionHandlingModeEnum.UseTypeDefault) field = default;
    }
    public void Deserialize<TValue, TPoly>(Godot.Collections.Dictionary dict, string fieldName, ref List<TValue> field) where TPoly : struct, IPolymorphicJsonSerializer<TValue>
    {
        string propertyName = GetPropertyName(fieldName);
        if (dict?.TryGetValue(propertyName, out var v) ?? false)
        {
            var list = field ?? new List<TValue>();
            list.Clear();
            foreach (var rawValue in v.AsGodotArray())
            {
                var value = new TPoly().Deserialize(rawValue);
                list.Add(value);
            }
            field = list;
        }
        else if(PropertyOmissionHandlingMode == PropertyOmissionHandlingModeEnum.UseTypeDefault) field = default;
    }
    public void Deserialize<TValue, TPoly>(Godot.Collections.Dictionary dict, string fieldName, ref TValue[] field) where TPoly : struct, IPolymorphicJsonSerializer<TValue>
    {
        string propertyName = GetPropertyName(fieldName);
        if (dict?.TryGetValue(propertyName, out var v) ?? false)
        {
            var list = new List<TValue>();
            list.Clear();
            foreach (var rawValue in v.AsGodotArray())
            {
                var value = new TPoly().Deserialize(rawValue);
                list.Add(value);
            }
            field = list.ToArray();
        }
        else if(PropertyOmissionHandlingMode == PropertyOmissionHandlingModeEnum.UseTypeDefault) field = default;
    }
    public void Deserialize<[MustBeVariant] T>(Godot.Collections.Dictionary dict, string fieldName, ref Array<T> field) where T : IJsonSerializable, new()
    {
        string propertyName = GetPropertyName(fieldName);
        if (dict?.TryGetValue(propertyName, out var v) ?? false)
        {
            var list = field ?? new Array<T>();
            list.Clear();
            foreach (var rawValue in v.AsGodotArray())
            {
                var value = new T();
                value.Deserialize(rawValue);
                list.Add(value);
            }
            field = list;
        }
        else if(PropertyOmissionHandlingMode == PropertyOmissionHandlingModeEnum.UseTypeDefault) field = default;
    }
    public void Deserialize<[MustBeVariant] TValue, TPoly>(Godot.Collections.Dictionary dict, string fieldName, ref Array<TValue> field) where TPoly : struct, IPolymorphicJsonSerializer<TValue>
    {
        string propertyName = GetPropertyName(fieldName);
        if (dict?.TryGetValue(propertyName, out var v) ?? false)
        {
            var list = field ?? new Array<TValue>();
            list.Clear();
            foreach (var rawValue in v.AsGodotArray())
            {
                var value = new TPoly().Deserialize(rawValue);
                list.Add(value);
            }
            field = list;
        }
        else if(PropertyOmissionHandlingMode == PropertyOmissionHandlingModeEnum.UseTypeDefault) field = default;
    }
    public void DeserializeVariants<[MustBeVariant] TKey, [MustBeVariant]T>(Godot.Collections.Dictionary dict, string fieldName, ref System.Collections.Generic.Dictionary<TKey, T> field)
    {
        string propertyName = GetPropertyName(fieldName);
        if (dict?.TryGetValue(propertyName, out var v) ?? false)
        {
            var valueDict = field ?? new System.Collections.Generic.Dictionary<TKey, T>();
            valueDict.Clear();
            foreach (var (key, rawValue) in v.AsGodotDictionary<TKey, Variant>())
            {
                valueDict[key] = rawValue.As<T>();
            }
            field = valueDict;
        }
        else if(PropertyOmissionHandlingMode == PropertyOmissionHandlingModeEnum.UseTypeDefault) field = default;
    }
    public void Deserialize<[MustBeVariant] TKey, T>(Godot.Collections.Dictionary dict, string fieldName, ref System.Collections.Generic.Dictionary<TKey, T> field) where T : IJsonSerializable, new()
    {
        string propertyName = GetPropertyName(fieldName);
        if (dict?.TryGetValue(propertyName, out var v) ?? false)
        {
            var valueDict = field ?? new System.Collections.Generic.Dictionary<TKey, T>();
            valueDict.Clear();
            foreach (var (key, rawValue) in v.AsGodotDictionary<TKey, Variant>())
            {
                var value = new T();
                value.Deserialize(rawValue);
                valueDict[key] = value;
            }
            field = valueDict;
        }
        else if(PropertyOmissionHandlingMode == PropertyOmissionHandlingModeEnum.UseTypeDefault) field = default;
    }
    public void Deserialize<[MustBeVariant] TKey, T>(Godot.Collections.Dictionary dict, string fieldName, ref System.Collections.Generic.Dictionary<TKey, List<T>> field) where T : IJsonSerializable, new()
    {
        string propertyName = GetPropertyName(fieldName);
        if (dict?.TryGetValue(propertyName, out var v) ?? false)
        {
            var valueDict = field ?? new System.Collections.Generic.Dictionary<TKey, List<T>>();
            valueDict.Clear();
            foreach (var (key, rawArray) in v.AsGodotDictionary<TKey, Variant>())
            {
                var list = new List<T>();
                
                foreach (var rawValue in rawArray.AsGodotArray())
                {
                    var value = new T();
                    value.Deserialize(rawValue);
                    list.Add(value);
                }
                
                valueDict[key] = list;
            }
            field = valueDict;
        }
        else if(PropertyOmissionHandlingMode == PropertyOmissionHandlingModeEnum.UseTypeDefault) field = default;
    }
    public void Deserialize<TValue, TPoly>(Godot.Collections.Dictionary dict, string fieldName, ref System.Collections.Generic.Dictionary<string, TValue> field) where TPoly : struct, IPolymorphicJsonSerializer<TValue>
    {
        string propertyName = GetPropertyName(fieldName);
        if (dict?.TryGetValue(propertyName, out var v) ?? false)
        {
            var valueDict = field ?? new System.Collections.Generic.Dictionary<string, TValue>();
            valueDict.Clear();
            foreach ((string key, var rawValue) in v.AsGodotDictionary<string, Variant>())
            {
                var value = new TPoly().Deserialize(rawValue);
                valueDict[key] = value;
            }
            field = valueDict;
        }
        else if(PropertyOmissionHandlingMode == PropertyOmissionHandlingModeEnum.UseTypeDefault) field = default;
    }
    
    public void SerializeVariant<[MustBeVariant]T>(Godot.Collections.Dictionary dict, string fieldName, ref T field)
    {
        string propertyName = GetPropertyName(fieldName);
        var serialized = Variant.From(field);
        if (serialized.VariantType != Variant.Type.Nil)
            dict[propertyName] = serialized;
        else
            dict.Remove(propertyName);
    }
    public void SerializeVariants<[MustBeVariant]T>(Godot.Collections.Dictionary dict, string fieldName, ref List<T> field)
    {
        string propertyName = GetPropertyName(fieldName);
        if (field is {Count: > 0})
        {
            var arr = new Godot.Collections.Array();
            foreach (var value in field)
            {
                arr.Add(Variant.From(value));
            }

            dict[propertyName] = arr;
        }
        else dict.Remove(propertyName);
    }
    public void SerializeEnum<[MustBeVariant]T>(Godot.Collections.Dictionary dict, string fieldName, ref T field) where T : struct, Enum
    {
        string propertyName = GetPropertyName(fieldName);
        bool isFlags = typeof(T).GetCustomAttribute<FlagsAttribute>() != null;
        if (isFlags)
        {
            dict[propertyName] = Variant.From(field);
        }
        else
        {
            switch (EnumSerializationMode)
            {
                case EnumSerializationModeEnum.SaveAsString:
                {
                    string serialized = Enum.GetName(field)?.ToSnakeCase();
                    if (serialized != null)
                        dict[propertyName] = serialized;
                    else
                        dict.Remove(propertyName);
                    break;
                }
                case EnumSerializationModeEnum.SaveAsInt:
                default:
                    dict[propertyName] = Variant.From(field);
                    break;
            }
        }
    }
    public void SerializeEnum<[MustBeVariant]T>(Godot.Collections.Dictionary dict, string fieldName, ref T? field) where T : struct, Enum
    {
        string propertyName = GetPropertyName(fieldName);
        bool isFlags = typeof(T).GetCustomAttribute<FlagsAttribute>() != null;
        if (field.HasValue)
        {
            if (isFlags)
            {
                dict[propertyName] = Variant.From(field.Value);
            }
            else
            {
                switch (EnumSerializationMode)
                {
                    case EnumSerializationModeEnum.SaveAsString:
                    {
                        string serialized = Enum.GetName(field.Value)?.ToSnakeCase();
                        if (serialized != null)
                            dict[propertyName] = serialized;
                        else
                            dict.Remove(propertyName);
                        break;
                    }
                    case EnumSerializationModeEnum.SaveAsInt:
                    default:
                        dict[propertyName] = Variant.From(field.Value);
                        break;
                }
            }
        }
        else
        {
            dict.Remove(propertyName);
        }
    }
    public void SerializeVariantNullable<[MustBeVariant]T>(Godot.Collections.Dictionary dict, string fieldName, ref T? field) where T : struct
    {
        string propertyName = GetPropertyName(fieldName);
        if (field.HasValue) dict[propertyName] = Variant.From(field.Value);
        else dict?.Remove(propertyName);
    }
    public void Serialize<T>(Godot.Collections.Dictionary dict, string fieldName, ref T field) where T : IJsonSerializable, new()
    {
        string propertyName = GetPropertyName(fieldName);
        if (field != null)
        {
            var serialized = field.Serialize();
            if (serialized.VariantType != Variant.Type.Nil)
                dict[propertyName] = serialized;
            else dict.Remove(propertyName);
        }
        else dict.Remove(propertyName);
    }
    public void Serialize<T>(Godot.Collections.Dictionary dict, string fieldName, ref T? field) where T : struct, IJsonSerializable
    {
        string propertyName = GetPropertyName(fieldName);
        if (field != null)
        {
            var serialized = field.Value.Serialize();
            if (serialized.VariantType != Variant.Type.Nil)
                dict[propertyName] = serialized;
            else dict.Remove(propertyName);
        }
        else dict.Remove(propertyName);
    }
    public void Serialize<TValue, TPoly>(Godot.Collections.Dictionary dict, string fieldName, ref TValue field) where TPoly : struct, IPolymorphicJsonSerializer<TValue>
    {
        string propertyName = GetPropertyName(fieldName);
        if (field != null)
        {
            var serialized = new TPoly().Serialize(field);
            if (serialized.VariantType != Variant.Type.Nil)
                dict[propertyName] = serialized;
            else dict.Remove(propertyName);
        }
        else dict.Remove(propertyName);
    }
    public void Serialize<T>(Godot.Collections.Dictionary dict, string fieldName, ref List<T> field) where T : IJsonSerializable, new()
    {
        string propertyName = GetPropertyName(fieldName);
        if (field is {Count: > 0})
        {
            var arr = new Godot.Collections.Array();
            foreach (var value in field)
            {
                var serialized = value.Serialize();
                arr.Add(serialized);
            }

            dict[propertyName] = arr;
        }
        else dict.Remove(propertyName);
    }
    public void Serialize<T>(Godot.Collections.Dictionary dict, string fieldName, ref T[] field) where T : IJsonSerializable, new()
    {
        string propertyName = GetPropertyName(fieldName);
        if (field is {Length: > 0})
        {
            var arr = new Godot.Collections.Array();
            foreach (var value in field)
            {
                var serialized = value.Serialize();
                arr.Add(serialized);
            }

            dict[propertyName] = arr;
        }
        else dict.Remove(propertyName);
    }
    public void Serialize<TValue, TPoly>(Godot.Collections.Dictionary dict, string fieldName, ref List<TValue> field) where TPoly : struct, IPolymorphicJsonSerializer<TValue>
    {
        string propertyName = GetPropertyName(fieldName);
        if (field is {Count: > 0})
        {
            var arr = new Godot.Collections.Array();
            foreach (var value in field)
            {
                var serialized = new TPoly().Serialize(value);
                arr.Add(serialized);
            }

            dict[propertyName] = arr;
        }
        else dict.Remove(propertyName);
    }
    public void Serialize<TValue, TPoly>(Godot.Collections.Dictionary dict, string fieldName, ref TValue[] field) where TPoly : struct, IPolymorphicJsonSerializer<TValue>
    {
        string propertyName = GetPropertyName(fieldName);
        if (field is {Length: > 0})
        {
            var arr = new Godot.Collections.Array();
            foreach (var value in field)
            {
                var serialized = new TPoly().Serialize(value);
                arr.Add(serialized);
            }

            dict[propertyName] = arr;
        }
        else dict.Remove(propertyName);
    }
    public void Serialize<[MustBeVariant] T>(Godot.Collections.Dictionary dict, string fieldName, ref Array<T> field) where T : IJsonSerializable, new()
    {
        string propertyName = GetPropertyName(fieldName);
        if (field is {Count: > 0})
        {
            var arr = new Godot.Collections.Array();
            foreach (var value in field)
            {
                var serialized = value.Serialize();
                arr.Add(serialized);
            }

            dict[propertyName] = arr;
        }
        else dict.Remove(propertyName);
    }
    public void Serialize<[MustBeVariant] TValue, TPoly>(Godot.Collections.Dictionary dict, string fieldName, ref Array<TValue> field) where TPoly : struct, IPolymorphicJsonSerializer<TValue>
    {
        string propertyName = GetPropertyName(fieldName);
        if (field is {Count: > 0})
        {
            var arr = new Godot.Collections.Array();
            foreach (var value in field)
            {
                var serialized = new TPoly().Serialize(value);
                arr.Add(serialized);
            }

            dict[propertyName] = arr;
        }
        else dict.Remove(propertyName);
    }
    public void SerializeVariants<[MustBeVariant]TKey, [MustBeVariant]T>(Godot.Collections.Dictionary dict, string fieldName, ref System.Collections.Generic.Dictionary<TKey, T> field)
    {
        string propertyName = GetPropertyName(fieldName);
        if (field is {Count: > 0})
        {
            var arr = new Godot.Collections.Dictionary();
            foreach (var (key, value) in field)
            {
                arr[Variant.From(key)] = Variant.From(value);
            }

            dict[propertyName] = arr;
        }
        else dict.Remove(propertyName);
    }
    public void Serialize<[MustBeVariant]TKey, T>(Godot.Collections.Dictionary dict, string fieldName, ref System.Collections.Generic.Dictionary<TKey, T> field) where T : IJsonSerializable, new()
    {
        string propertyName = GetPropertyName(fieldName);
        if (field is {Count: > 0})
        {
            var arr = new Godot.Collections.Dictionary();
            foreach (var (key, value) in field)
            {
                var serialized = value.Serialize();
                arr[Variant.From(key)] = serialized;
            }

            dict[propertyName] = arr;
        }
        else dict.Remove(propertyName);
    }
    public void Serialize<[MustBeVariant]TKey, T>(Godot.Collections.Dictionary dict, string fieldName, ref System.Collections.Generic.Dictionary<TKey, List<T>> field) where T : IJsonSerializable, new()
    {
        string propertyName = GetPropertyName(fieldName);
        if (field is {Count: > 0})
        {
            var arr = new Godot.Collections.Dictionary();
            foreach (var (key, value) in field)
            {
                var valueArr = new Godot.Collections.Array();
                foreach (var element in value)
                {
                    var elementSerialized = element.Serialize();
                    valueArr.Add(elementSerialized);
                }

                arr[Variant.From(key)] = valueArr;
            }

            dict[propertyName] = arr;
        }
        else dict.Remove(propertyName);
    }
    public void Serialize<TValue, TPoly>(Godot.Collections.Dictionary dict, string fieldName, ref System.Collections.Generic.Dictionary<string, TValue> field) where TPoly : struct, IPolymorphicJsonSerializer<TValue>
    {
        string propertyName = GetPropertyName(fieldName);
        if (field is {Count: > 0})
        {
            var arr = new Godot.Collections.Dictionary();
            foreach ((string key, var value) in field)
            {
                var serialized = new TPoly().Serialize(value);
                arr[key] = serialized;
            }

            dict[propertyName] = arr;
        }
        else dict.Remove(propertyName);
    }

    public enum CaseConversionModeEnum
    {
        Keep,
        UseSnakeCase
    }
    public enum PropertyOmissionHandlingModeEnum
    {
        UseTypeDefault,
        KeepDefault
    }
    public enum EnumSerializationModeEnum
    {
        SaveAsString,
        SaveAsInt
    }
    public enum EnumDeserializationModeEnum
    {
        Strict,
        Lenient
    }

    public static class ProtectedMethods
    {
        public static string GetPropertyName(JsonSerializer json, string targetName)
        {
            return json.GetPropertyName(targetName);
        }
    }
}