namespace GDF.Multiplayer;

public partial class Room
{
    private int _nextObjectId = 0;

    public string GenerateUniqueObjectName(string label = null)
    {
        return $"{label ?? "Object"} [{PeerId:X}_{_nextObjectId++}]";
    }
}