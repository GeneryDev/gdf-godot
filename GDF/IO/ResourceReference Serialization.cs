using GDF.Serialization;
using Godot;

namespace GDF.IO;

public partial class ResourceReference : IJsonSerializable
{
    public void Deserialize(Variant v)
    {
        var dict = v.AsGodotDictionary();
        var json = JsonSerializer.Default with
        {
            PropertyOmissionHandlingMode = JsonSerializer.PropertyOmissionHandlingModeEnum.KeepDefault
        };
        json.Deserialize(dict, "id", ref StoredResourceId);
        json.Deserialize(dict, "path", ref StoredResourcePath);
    }

    public Variant Serialize()
    {
        var dict = new Godot.Collections.Dictionary();
        var json = JsonSerializer.Default with
        {
            PropertyOmissionHandlingMode = JsonSerializer.PropertyOmissionHandlingModeEnum.KeepDefault
        };
        json.Serialize(dict, "id", ref StoredResourceId);
        json.Serialize(dict, "path", ref StoredResourcePath);
        return dict;
    }
}