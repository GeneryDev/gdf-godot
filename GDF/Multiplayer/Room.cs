using GDF.Util;
using Godot;

namespace GDF.Multiplayer;

public abstract partial class Room : SingletonNode<Room>
{
    public abstract int PeerId { get; }
    public abstract bool IsOnline { get; }
}