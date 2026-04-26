using Godot;

namespace GDF.Networking;

public class RpcChannelPreset
{
    public int Channel;
    public MultiplayerPeer.TransferModeEnum TransferMode;

    public RpcChannelPreset(int channel, MultiplayerPeer.TransferModeEnum transferMode = MultiplayerPeer.TransferModeEnum.Reliable)
    {
        Channel = channel;
        TransferMode = transferMode;
    }
    
    public static implicit operator int(RpcChannelPreset preset)
    {
        return preset.Channel;
    }
    
    public static implicit operator MultiplayerPeer.TransferModeEnum(RpcChannelPreset preset)
    {
        return preset.TransferMode;
    }
}