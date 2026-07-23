using System.Reflection;
using GDF.Multiplayer;
using GDF.Util;
using Godot;
using Godot.Collections;

namespace GDF.Networking;

[GlobalClass]
[SingletonUsage(SingletonUsage.Autoload)]
public partial class GdfRpcSystem : SingletonNode<GdfRpcSystem>
{
    private System.Collections.Generic.Dictionary<string, GdfRpcChannelInstance> _instancesByPresetName;
    
    private void SetupPresets()
    {
        if (_instancesByPresetName != null) return;
        _instancesByPresetName = new();
        var channelsClass = typeof(RpcChannels);
        var presetClass = typeof(RpcChannelPreset);
        var fields = channelsClass.GetFields(BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.Public);
        foreach (var field in fields)
        {
            if (field.FieldType != presetClass) continue;
            string name = field.Name;
            var value = (RpcChannelPreset) field.GetValue(null);
            if (value == null) continue;

            var instance = new GdfRpcChannelInstance()
            {
                Channel = value.Channel,
                TransferMode = value.TransferMode,
                Name = name,
                System = this
            };
            instance.ConfigureRpc();

            _instancesByPresetName[name] = instance;
            this.AddChild(instance);
        }
    }
    
    public void Send(Node node, int peerId, StringName methodName, Variant[] args)
    {
        if (node == null) return;

        if (GdfRpcAttribute.GetAttribute(node, methodName) is not {} attr)
        {
            GD.PushError(
                $"Attempted to send an illegal GdfRpc call on node type '{node.GetType()}', method '{methodName}'."
#if TOOLS
                + "\nTo enable this method for GdfRpc, add the [GdfRpc] attribute to the method."
#endif
            );
            return;
        }

        if (!_instancesByPresetName.TryGetValue(attr.ChannelPresetName, out var channelInstance))
        {
            GD.PushError(
                $"Failed to send GdfRpc call on node type '{node.GetType()}', method '{methodName}'. No such channel preset '{attr.ChannelPresetName}'"
#if TOOLS
                + "\nTo fix, ensure the name in the [GdfRpc] attribute matches a field defined in the RpcChannels class (in GdfConstants.cs)."
#endif
            );
            return;
        }

        if (attr.Mode == MultiplayerApi.RpcMode.Disabled) return;
        int ownId = Multiplayer.GetUniqueId();
        if (attr.Mode == MultiplayerApi.RpcMode.Authority && !node.IsMultiplayerAuthority())
        {
            GD.PushError(
                $"Failed to send GdfRpc call on node type '{node.GetType()}', method '{methodName}'. Node is not the authority (expected {node.GetMultiplayerAuthority()}, is {ownId})"
            );
            return;
        }

        if (!attr.CallLocal)
        {
            if (peerId == ownId) return; // do not call
            else if (peerId == 0) peerId = -ownId; // call on everyone except self
            else if (peerId < 0)
            {
                // asked to call on everyone except a peer other than self, cannot exclude self. Rely on the Receive method catching this.
            }
        }

        if (SerializeArgs(out var serializedArgs, out ulong argFlags, args))
            channelInstance.RpcId(peerId, GdfRpcChannelInstance.MethodName.Receive, node.GetPath(), methodName, serializedArgs, argFlags);
    }
    
    public static void SendOffline(Node node, int peerId, StringName methodName, Variant[] args)
    {
        if (node == null) return;

        if (GdfRpcAttribute.GetAttribute(node, methodName) is not {} attr)
        {
            GD.PushError(
                $"Attempted to send an illegal GdfRpc call on node type '{node.GetType()}', method '{methodName}'."
#if TOOLS
                + "\nTo enable this method for GdfRpc, add the [GdfRpc] attribute to the method."
#endif
            );
            return;
        }

        if (attr.Mode == MultiplayerApi.RpcMode.Disabled) return;
        int ownId = node.Multiplayer.GetUniqueId();
        if (attr.Mode == MultiplayerApi.RpcMode.Authority && !node.IsMultiplayerAuthority())
        {
            GD.PushError(
                $"Failed to send GdfRpc call on node type '{node.GetType()}', method '{methodName}'. Node is not the authority (expected {node.GetMultiplayerAuthority()}, is {ownId})"
            );
            return;
        }

        if (!attr.CallLocal) return;
        
        if (peerId > 0 && peerId != ownId)
        {
            // asked to call someone specifically who isn't oneself; don't call
            return;
        }
        if (peerId < 0 && peerId == -ownId)
        {
            // asked to call everyone except specifically oneself; don't call
            return;
        }
        node.Call(methodName, args);
    }

    public void Receive(Node node, StringName methodName, Array args, ulong argFlags)
    {
        if (node == null)
        {
            // Node is not found in tree, ignore
            return;
        }
        
        int senderId = Multiplayer.GetRemoteSenderId();
        int ownId = Multiplayer.GetUniqueId();

        if (GdfRpcAttribute.GetAttribute(node, methodName) is not {} attr)
        {
            GD.PushError(
                $"Received an illegal GdfRpc call on node type '{node.GetType()}', method '{methodName}'."
#if TOOLS
                + "\nTo enable this method for GdfRpc, add the [GdfRpc] attribute to the method."
#endif
            );
            return;
        }

        if (!attr.CallLocal && senderId == ownId)
        {
            // do not handle
            return;
        }

        if (attr.Mode == MultiplayerApi.RpcMode.Disabled) return;
        if (attr.Mode == MultiplayerApi.RpcMode.Authority && node.GetMultiplayerAuthority() != senderId)
        {
            GD.PushError(
                $"Received an illegal GdfRpc call on node type '{node.GetType()}', method '{methodName}'. Remote sender is not the authority of the local node (expected {node.GetMultiplayerAuthority()}, is {senderId})"
            );
            return;
        }
        
        DeserializeArgs(args, argFlags);

        node.Callv(methodName, args);
    }

    public override void _Notification(int what)
    {
        if(what == NotificationParented) SetupPresets();
    }
}

public static class GdfRpcNodeExtensions
{
    public static void GdfRpc(this Node node, StringName method, params Variant[] args)
    {
        if (GdfRpcSystem.InstanceExists)
        {
            GdfRpcSystem.Instance?.Send(node, 0, method, args);
        }
        else
        {
            GdfRpcSystem.SendOffline(node, 0, method, args);
        }
    }
    public static void GdfRpcId(this Node node, int peerId, StringName method, params Variant[] args)
    {
        if (GdfRpcSystem.InstanceExists)
        {
            GdfRpcSystem.Instance?.Send(node, peerId, method, args);
        }
        else
        {
            GdfRpcSystem.SendOffline(node, peerId, method, args);
        }
    }
}