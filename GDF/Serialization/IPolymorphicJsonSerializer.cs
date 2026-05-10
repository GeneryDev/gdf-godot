using Godot;

namespace GDF.Serialization;

public interface IPolymorphicJsonSerializer<T>
{
    public T Deserialize(Variant v);
    public Variant Serialize(T obj);
}