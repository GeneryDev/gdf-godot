using Godot;

namespace GDF.Serialization;

public interface IJsonSerializable
{
    public void Deserialize(Variant v);
    public Variant Serialize();
}