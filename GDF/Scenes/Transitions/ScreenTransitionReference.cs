using GDF.Data;
using GDF.Serialization;
using Godot;

namespace GDF.Scenes.Transitions;

[GlobalClass]
public partial class ScreenTransitionReference : Resource, IDataContext, IJsonSerializable
{
    [Export] public StringName TransitionId = "";

    bool IDataContext.GetContextVariable(string key, string input, ref Variant output,
        IDataQueryOptions options)
    {
        switch (key)
        {
            case "transition_id":
            {
                output = TransitionId;
                return true;
            }
            default:
            {
                if (HasMeta(key))
                {
                    output = GetMeta(key);
                    return true;
                }

                return false;
            }
        }
    }

    public void Deserialize(Variant v)
    {
        var dict = v.AsGodotDictionary();
        var json = JsonSerializer.Default with
        {
            PropertyOmissionHandlingMode = JsonSerializer.PropertyOmissionHandlingModeEnum.KeepDefault
        };
        json.Deserialize(dict, nameof(TransitionId), ref TransitionId);
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
        json.Serialize(dict, nameof(TransitionId), ref TransitionId);
        foreach(var metaName in this.GetMetaList())
        {
            Variant value = default;
            json.SerializeVariant(dict, metaName, ref value);
        }
        return dict;
    }
}