using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;

namespace GDF.Networking;

[AttributeUsage(AttributeTargets.Method)]
public class CustomRpcAttribute : Attribute
{
    private static readonly Dictionary<Type, Dictionary<StringName, CustomRpcAttribute>> AllowedMethodsByType = new();
    
    public readonly string ChannelPresetName;
    public MultiplayerApi.RpcMode Mode = MultiplayerApi.RpcMode.AnyPeer;
    public bool CallLocal = true;

    public CustomRpcAttribute(string channelPresetName = GdfConstants.DefaultRpcChannelPresetName)
    {
        ChannelPresetName = channelPresetName;
    }

    public static CustomRpcAttribute GetAttribute(Node node, StringName methodName)
    {
        var receivingType = node.GetType();
        if (AllowedMethodsByType.TryGetValue(receivingType, out var existingMap))
            return existingMap.GetValueOrDefault(methodName);

        var newMap = new Dictionary<StringName, CustomRpcAttribute>();
        foreach (var method in receivingType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                                        BindingFlags.Instance))
            if (method.GetCustomAttribute<CustomRpcAttribute>(true) is {} attr)
                newMap[method.Name] = attr;

        AllowedMethodsByType[receivingType] = newMap;
        return newMap.GetValueOrDefault(methodName);
    }
}