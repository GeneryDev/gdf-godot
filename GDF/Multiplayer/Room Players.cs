using System.Collections.Generic;
using Godot;

namespace GDF.Multiplayer;

public partial class Room
{
    public int TotalPlayerCount => GetAllPlayerInfo().Count;
    public int LocalPlayerCount => GetPlayerCountForPeer(PeerId);
    public bool IsSpectating => LocalPlayerCount <= 0;

    public abstract List<PlayerInfo> GetAllPlayerInfo();

    public PlayerInfo? GetPlayerInfo(int playerId)
    {
        foreach (var playerInfo in GetAllPlayerInfo())
            if (playerInfo.PlayerId == playerId)
                return playerInfo;
        
        return null;
    }

    public bool TryGetPlayerInfo(int playerId, out PlayerInfo playerInfo)
    {
        var infoOpt = GetPlayerInfo(playerId);
        if (infoOpt != null)
        {
            playerInfo = infoOpt.Value;
            return true;
        }
        else
        {
            playerInfo = default;
            return false;
        }
    }

    public bool HasPlayer(int playerId)
    {
        return GetPlayerInfo(playerId) != null;
    }

    public int GetPlayerIndex(int playerId)
    {
        var players = GetAllPlayerInfo();
        for (var i = 0; i < players.Count; i++)
            if (players[i].PlayerId == playerId)
                return i;

        return -1;
    }

    public int GetFirstLocalPlayerId()
    {
        foreach (var playerInfo in GetAllPlayerInfo())
        {
            if (!playerInfo.OwnedByThisClient) continue;
            return playerInfo.PlayerId;
        }

        return -1;
    }

    public int GetOnlyLocalPlayerId()
    {
        foreach (var playerInfo in GetAllPlayerInfo())
        {
            if (!playerInfo.OwnedByThisClient) continue;
            return playerInfo.PlayerId;
        }

        return -1;
    }

    public int GetPlayerCountForPeer(int peerId)
    {
        var count = 0;
        foreach (var playerInfo in GetAllPlayerInfo())
            if (playerInfo.PeerId == peerId)
                count++;
        return count;
    }

    public struct PlayerInfo
    {
        public int PeerId;
        public int PlayerId;
        public int IndexInClient;
        public int PlayerIndex => Instance.GetPlayerIndex(PlayerId);
        public bool OwnedByThisClient => PeerId == Instance.PeerId;

        public Variant Serialize()
        {
            return new Vector3I(PeerId, PlayerId, IndexInClient);
        }

        public static PlayerInfo Deserialize(Variant v)
        {
            var vec = v.AsVector3I();
            return new PlayerInfo()
            {
                PeerId = vec.X,
                PlayerId = vec.Y,
                IndexInClient = vec.Z
            };
        }
    }
}