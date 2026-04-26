using GDF.Networking;

namespace GDF;

/// <summary>
/// Holds user-configurable constants that affect GDF behavior.
/// </summary>
public static class GdfConstants
{
    /// <summary>
    /// Where is this plugin installed?
    /// </summary>
    public const string PluginRoot = "res://addons/gdf-godot";
    
    /// <summary>
    /// Where are this plugin's icons stored?
    /// </summary>
    public const string IconRoot = $"{PluginRoot}/assets/icons";
    
    /// <summary>
    /// What RPC transfer channel should logical nodes use when replicating to other peers?
    /// </summary>
    public const int DefaultRpcTransferChannel = 1;

    /// <summary>
    /// Notification received by all the nodes in the newly instantiated scene, when <see cref="GDF.Util.PackedSceneExtensions.GdfInstantiate"/> is completed.
    /// </summary>
    // (Change if this conflicts with another plugin notification, or a newer Godot notification)
    public const int NotificationDeepSceneInstantiated = 5012;
}

/// <summary>
/// Holds user-configurable, named, RPC presets. These are used for Custom RPCs.
/// These presets are found through Reflection.
/// </summary>
public static class RpcChannels
{
    public static readonly RpcChannelPreset Default = new(GdfConstants.DefaultRpcTransferChannel);
}