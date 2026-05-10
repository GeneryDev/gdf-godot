using GDF.Util;
using Godot;

namespace GDF.Multiplayer;

[GlobalClass]
public abstract partial class Room : SingletonNode<Room>
{
    [Signal]
    public delegate void PlayerJoinedEventHandler(int playerId);
    [Signal]
    public delegate void PlayerLeftEventHandler(int playerId);
    [Signal]
    public delegate void PeerConnectedEventHandler(int peerId);
    [Signal]
    public delegate void PeerDisconnectedEventHandler(int peerId);
    
    public abstract int PeerId { get; }
    public abstract bool IsOnline { get; }
}