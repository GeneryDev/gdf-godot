using Godot;

namespace GDF.Serialization;

public partial struct JsonSerializer
{
    public void Deserialize(Godot.Collections.Dictionary dict, string fieldName, ref Vector3 field)
    {
        string propertyName = GetPropertyName(fieldName);
        if ((dict?.TryGetValue(propertyName, out var v) ?? false) && v.VariantType == Variant.Type.Array)
        {
            var value = new Vector3();
            float[] raw = v.AsFloat32Array();
            if (raw.Length > 0) value.X = raw[0];
            if (raw.Length > 1) value.Y = raw[1];
            if (raw.Length > 2) value.Z = raw[2];
            field = value;
        }
        else if(PropertyOmissionHandlingMode == PropertyOmissionHandlingModeEnum.UseTypeDefault) field = default;
    }
    public void Serialize(Godot.Collections.Dictionary dict, string fieldName, ref Vector3 field)
    {
        string propertyName = GetPropertyName(fieldName);
        var serialized = new float[] {field.X, field.Y, field.Z};
        dict[propertyName] = serialized;
    }
    
    public void Deserialize(Godot.Collections.Dictionary dict, string fieldName, ref Vector2 field)
    {
        string propertyName = GetPropertyName(fieldName);
        if ((dict?.TryGetValue(propertyName, out var v) ?? false) && v.VariantType == Variant.Type.Array)
        {
            var value = new Vector2();
            float[] raw = v.AsFloat32Array();
            if (raw.Length > 0) value.X = raw[0];
            if (raw.Length > 1) value.Y = raw[1];
            field = value;
        }
        else if(PropertyOmissionHandlingMode == PropertyOmissionHandlingModeEnum.UseTypeDefault) field = default;
    }
    public void Serialize(Godot.Collections.Dictionary dict, string fieldName, ref Vector2 field)
    {
        string propertyName = GetPropertyName(fieldName);
        var serialized = new float[] {field.X, field.Y};
        dict[propertyName] = serialized;
    }
    public void Deserialize(Godot.Collections.Dictionary dict, string fieldName, ref Vector3I field)
    {
        string propertyName = GetPropertyName(fieldName);
        if ((dict?.TryGetValue(propertyName, out var v) ?? false) && v.VariantType == Variant.Type.Array)
        {
            var value = new Vector3I();
            int[] raw = v.AsInt32Array();
            if (raw.Length > 0) value.X = raw[0];
            if (raw.Length > 1) value.Y = raw[1];
            if (raw.Length > 2) value.Z = raw[2];
            field = value;
        }
        else if(PropertyOmissionHandlingMode == PropertyOmissionHandlingModeEnum.UseTypeDefault) field = default;
    }
    public void Serialize(Godot.Collections.Dictionary dict, string fieldName, ref Vector3I field)
    {
        string propertyName = GetPropertyName(fieldName);
        var serialized = new float[] {field.X, field.Y, field.Z};
        dict[propertyName] = serialized;
    }
    
    public void Deserialize(Godot.Collections.Dictionary dict, string fieldName, ref Vector2I field)
    {
        string propertyName = GetPropertyName(fieldName);
        if ((dict?.TryGetValue(propertyName, out var v) ?? false) && v.VariantType == Variant.Type.Array)
        {
            var value = new Vector2I();
            int[] raw = v.AsInt32Array();
            if (raw.Length > 0) value.X = raw[0];
            if (raw.Length > 1) value.Y = raw[1];
            field = value;
        }
        else if(PropertyOmissionHandlingMode == PropertyOmissionHandlingModeEnum.UseTypeDefault) field = default;
    }
    public void Serialize(Godot.Collections.Dictionary dict, string fieldName, ref Vector2I field)
    {
        string propertyName = GetPropertyName(fieldName);
        var serialized = new int[] {field.X, field.Y};
        dict[propertyName] = serialized;
    }
    
    public void Deserialize(Godot.Collections.Dictionary dict, string fieldName, ref Quaternion field)
    {
        string propertyName = GetPropertyName(fieldName);
        if ((dict?.TryGetValue(propertyName, out var v) ?? false) && v.VariantType == Variant.Type.Array)
        {
            var value = new Quaternion();
            float[] raw = v.AsFloat32Array();
            if (raw.Length > 0) value.X = raw[0];
            if (raw.Length > 1) value.Y = raw[1];
            if (raw.Length > 2) value.Z = raw[2];
            if (raw.Length > 3) value.W = raw[3];
            field = value;
        }
        else if(PropertyOmissionHandlingMode == PropertyOmissionHandlingModeEnum.UseTypeDefault) field = default;
    }
    public void Serialize(Godot.Collections.Dictionary dict, string fieldName, ref Quaternion field)
    {
        string propertyName = GetPropertyName(fieldName);
        var serialized = new float[] {field.X, field.Y, field.Z, field.W};
        dict[propertyName] = serialized;
    }
    
    public void Deserialize(Godot.Collections.Dictionary dict, string fieldName, ref Color field)
    {
        string propertyName = GetPropertyName(fieldName);
        if (dict?.TryGetValue(propertyName, out var v) ?? false)
        {
            if (v.VariantType == Variant.Type.Color) field = v.AsColor();
            else field = new Color(v.AsString());
        }
        else if(PropertyOmissionHandlingMode == PropertyOmissionHandlingModeEnum.UseTypeDefault) field = default;
    }
    public void Serialize(Godot.Collections.Dictionary dict, string fieldName, ref Color field)
    {
        string propertyName = GetPropertyName(fieldName);
        var serialized = field.ToHtml();
        dict[propertyName] = serialized;
    }
    
    
    
    
    // Simple serializers
    public void Deserialize(Godot.Collections.Dictionary dict, string fieldName,
        ref bool field) => DeserializeVariant(dict, fieldName, ref field);
    public void Serialize(Godot.Collections.Dictionary dict, string fieldName,
        ref bool field) => SerializeVariant(dict, fieldName, ref field);
    
    public void Deserialize(Godot.Collections.Dictionary dict, string fieldName,
        ref string field) => DeserializeVariant(dict, fieldName, ref field);
    public void Serialize(Godot.Collections.Dictionary dict, string fieldName,
        ref string field) => SerializeVariant(dict, fieldName, ref field);
    
    public void Deserialize(Godot.Collections.Dictionary dict, string fieldName,
        ref StringName field) => DeserializeVariant(dict, fieldName, ref field);
    public void Serialize(Godot.Collections.Dictionary dict, string fieldName,
        ref StringName field) => SerializeVariant(dict, fieldName, ref field);
    
    public void Deserialize(Godot.Collections.Dictionary dict, string fieldName,
        ref byte field) => DeserializeVariant(dict, fieldName, ref field);
    public void Serialize(Godot.Collections.Dictionary dict, string fieldName,
        ref byte field) => SerializeVariant(dict, fieldName, ref field);
    
    public void Deserialize(Godot.Collections.Dictionary dict, string fieldName,
        ref sbyte field) => DeserializeVariant(dict, fieldName, ref field);
    public void Serialize(Godot.Collections.Dictionary dict, string fieldName,
        ref sbyte field) => SerializeVariant(dict, fieldName, ref field);
    
    public void Deserialize(Godot.Collections.Dictionary dict, string fieldName,
        ref short field) => DeserializeVariant(dict, fieldName, ref field);
    public void Serialize(Godot.Collections.Dictionary dict, string fieldName,
        ref short field) => SerializeVariant(dict, fieldName, ref field);
    
    public void Deserialize(Godot.Collections.Dictionary dict, string fieldName,
        ref ushort field) => DeserializeVariant(dict, fieldName, ref field);
    public void Serialize(Godot.Collections.Dictionary dict, string fieldName,
        ref ushort field) => SerializeVariant(dict, fieldName, ref field);
    
    public void Deserialize(Godot.Collections.Dictionary dict, string fieldName,
        ref int field) => DeserializeVariant(dict, fieldName, ref field);
    public void Serialize(Godot.Collections.Dictionary dict, string fieldName,
        ref int field) => SerializeVariant(dict, fieldName, ref field);
    
    public void Deserialize(Godot.Collections.Dictionary dict, string fieldName,
        ref uint field) => DeserializeVariant(dict, fieldName, ref field);
    public void Serialize(Godot.Collections.Dictionary dict, string fieldName,
        ref uint field) => SerializeVariant(dict, fieldName, ref field);
    
    public void Deserialize(Godot.Collections.Dictionary dict, string fieldName,
        ref long field) => DeserializeVariant(dict, fieldName, ref field);
    public void Serialize(Godot.Collections.Dictionary dict, string fieldName,
        ref long field) => SerializeVariant(dict, fieldName, ref field);
    
    public void Deserialize(Godot.Collections.Dictionary dict, string fieldName,
        ref ulong field) => DeserializeVariant(dict, fieldName, ref field);
    public void Serialize(Godot.Collections.Dictionary dict, string fieldName,
        ref ulong field) => SerializeVariant(dict, fieldName, ref field);
    
    public void Deserialize(Godot.Collections.Dictionary dict, string fieldName,
        ref float field) => DeserializeVariant(dict, fieldName, ref field);
    public void Serialize(Godot.Collections.Dictionary dict, string fieldName,
        ref float field) => SerializeVariant(dict, fieldName, ref field);
    
    public void Deserialize(Godot.Collections.Dictionary dict, string fieldName,
        ref double field) => DeserializeVariant(dict, fieldName, ref field);
    public void Serialize(Godot.Collections.Dictionary dict, string fieldName,
        ref double field) => SerializeVariant(dict, fieldName, ref field);
}