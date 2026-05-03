using GDF.Multiplayer;
using Godot;

namespace GDF.Networking;

public interface IResynchronizable
{
    public static StringName MethodNameResync = nameof(Resync);

    // Attribute *must* be copied to all implementations of this method!!!
    [CustomRpc(GdfConstants.DefaultRpcChannelPresetName, CallLocal = false, Mode = MultiplayerApi.RpcMode.AnyPeer)]
    public void Resync(int peerId);
}

public static class ResynchronizableExt
{
    public static void Resync(this IResynchronizable resync)
    {
        if (resync is not Node node) return;
        if (node.IsMultiplayerAuthority())
            //Nothing to be done
            return;

        // Not multiplayer authority.
        // Send an RPC to the authority, asking for all info needed to resync.
        // Use custom RPC for error suppression.

        int authority = node.GetMultiplayerAuthority();
        if (Room.Instance.HasPeer(authority))
            node.CustomRpcId(authority, IResynchronizable.MethodNameResync, node.Multiplayer.GetUniqueId());
    }
}