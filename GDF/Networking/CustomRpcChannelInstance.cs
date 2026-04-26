using Godot;
using Godot.Collections;

namespace GDF.Networking;

public partial class CustomRpcChannelInstance : Node
{
    [Export] public int Channel;
    [Export] public MultiplayerPeer.TransferModeEnum TransferMode;

    public CustomRpcSystem System;

    public void ConfigureRpc()
    {
        var config = new Dictionary();
        config["rpc_mode"] = Variant.From(MultiplayerApi.RpcMode.AnyPeer);
        config["transfer_mode"] = Variant.From(TransferMode);
        config["call_local"] = true;
        config["channel"] = Channel;
        RpcConfig(MethodName.Receive, config);
    }

    // Config set via RpcConfig
    private void Receive(NodePath path, StringName methodName, Array args, uint argFlags)
    {
        // GD.Print($"[{Multiplayer.GetUniqueId()}] Receive custom RPC {methodName} from {Multiplayer.GetRemoteSenderId()}");
        var node = GetTree().Root.GetNodeOrNull(path);

        System.Receive(node, methodName, args, argFlags);
    }
}