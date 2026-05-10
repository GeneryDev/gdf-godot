using System.Collections.Generic;

namespace GDF.Multiplayer;

public partial class Room
{
    public abstract List<int> GetAllPeerIds();

    public bool HasPeer(int peerId)
    {
        return GetAllPeerIds()?.Contains(peerId) ?? false;
    }
}