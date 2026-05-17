using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;

namespace GDF.Networking;

[AttributeUsage(AttributeTargets.Method)]
public class GdfRpcAttribute : Attribute
{
    private static readonly Dictionary<Type, Dictionary<StringName, GdfRpcAttribute>> AllowedMethodsByType = new();
    
    public readonly string ChannelPresetName;
    public MultiplayerApi.RpcMode Mode = MultiplayerApi.RpcMode.AnyPeer;
    public bool CallLocal = true;

    public GdfRpcAttribute(string channelPresetName = GdfConstants.DefaultRpcChannelPresetName)
    {
        ChannelPresetName = channelPresetName;
    }

    public static GdfRpcAttribute GetAttribute(Node node, StringName methodName)
    {
        var receivingType = node.GetType();
        if (AllowedMethodsByType.TryGetValue(receivingType, out var existingMap))
            return existingMap.GetValueOrDefault(methodName);

        var newMap = new Dictionary<StringName, GdfRpcAttribute>();
        foreach (var method in receivingType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                                        BindingFlags.Instance))
            if (method.GetCustomAttribute<GdfRpcAttribute>(true) is {} attr)
                newMap[method.Name] = attr;

        AllowedMethodsByType[receivingType] = newMap;
        return newMap.GetValueOrDefault(methodName);
    }
}