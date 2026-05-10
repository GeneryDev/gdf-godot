using Godot;

namespace GDF.Resources;

public interface IJsonResource
{
    public string JsonFilePath { get; set; }
    public Error Parse(Json json);
}