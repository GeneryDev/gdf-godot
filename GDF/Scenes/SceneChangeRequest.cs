using GDF.IO;
using GDF.Serialization;
using Godot;

namespace GDF.Scenes;

public partial class SceneChangeRequest : Resource, IJsonSerializable
{
    [Export] public ResourceReference SceneReference;
    
    public ResourceReference CreateSceneReference()
    {
        return (ResourceReference)SceneReference.Duplicate();
    }

    public void Deserialize(Variant v)
    {
        var dict = v.AsGodotDictionary();
        var json = JsonSerializer.Default with
        {
            PropertyOmissionHandlingMode = JsonSerializer.PropertyOmissionHandlingModeEnum.KeepDefault
        };
        json.Deserialize(dict, "scene", ref SceneReference);
        foreach(var metaName in this.GetMetaList())
        {
            Variant value = default;
            json.DeserializeVariant(dict, metaName, ref value);
            SetMeta(metaName, value);
        }
    }

    public Variant Serialize()
    {
        var dict = new Godot.Collections.Dictionary();
        var json = JsonSerializer.Default with
        {
            PropertyOmissionHandlingMode = JsonSerializer.PropertyOmissionHandlingModeEnum.KeepDefault
        };
        json.Serialize(dict, "scene", ref SceneReference);
        foreach(var metaName in this.GetMetaList())
        {
            Variant value = default;
            json.SerializeVariant(dict, metaName, ref value);
        }
        return dict;
    }
    
    public static implicit operator SceneChangeRequest(ResourceReference sceneReference)
    {
        return new SceneChangeRequest()
        {
            SceneReference = sceneReference
        };
    }
}