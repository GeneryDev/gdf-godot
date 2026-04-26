using Godot;

namespace GDF.Logical;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/logic_in.png")]
public abstract partial class TriggerableLogicNode : Node
{
    [Signal]
    public delegate void TriggeredEventHandler();

    [ExportGroup("Tree")]
    [Export] public bool ExecuteOutsideTree = true;
    [ExportGroup("Networking")]
    [Export] public AuthorityMode AuthorityMode = AuthorityMode.AnyPeer;
    [Export] public bool ReplicateToPeers = false;
    [ExportGroup("Editor")]
    [Export] public bool RunInEditor = false;

    protected Empty HandleTrigger()
    {
        if (!RunInEditor && Engine.IsEditorHint()) return default;
        if (!ExecuteOutsideTree && !IsInsideTree()) return default;
        if (!AuthorityMode.CanExecute(this)) return default;

        if (ReplicateToPeers)
            Rpc(MethodName.TriggerRpc);
        else
            Execute();

        return default;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer,
        CallLocal = true,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,
        TransferChannel = GdfConstants.DefaultRpcTransferChannel)]
    private void TriggerRpc()
    {
        Execute();
    }

    protected virtual Empty Execute()
    {
        EmitSignalTriggered();
        return default;
    }

    /// <summary>
    /// Empty struct. Used as return value to prevent the Godot editor
    /// showing specific methods in the method selection popup,
    /// or otherwise being handled as a Godot method.
    /// </summary>
    protected struct Empty
    {
    }
}