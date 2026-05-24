using GDF.Logical.Values;
using Godot;

namespace GDF.Multiplayer;

[GlobalClass]
public partial class UniqueObjectName : ValueSource
{
    [Export] public string Label = "Object";
    
    public override Variant GetValue(Node source)
    {
        return Room.Instance.GenerateUniqueObjectName(Label);
    }
}