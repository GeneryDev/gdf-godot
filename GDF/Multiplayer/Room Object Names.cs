namespace GDF.Multiplayer;

public partial class Room
{
    private int _nextObjectId = 0;

    public string ClaimNewObjectName(string type = null)
    {
        return $"{type ?? "Object"} [{PeerId:X}_{_nextObjectId++}]";
    }
}