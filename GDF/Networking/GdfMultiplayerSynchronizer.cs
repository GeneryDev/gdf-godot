using Godot;

namespace GDF.Networking;

[GlobalClass]
public partial class GdfMultiplayerSynchronizer : MultiplayerSynchronizer
{
    private bool _reentryQueued = false;
    [Export] public bool FixLateEntrySyncing = true;
    [Export] public bool ReplicationConfigLocalToScene = false;

    public override void _Ready()
    {
        base._Ready();
        if (ReplicationConfigLocalToScene) ReplicationConfig = (SceneReplicationConfig)ReplicationConfig?.Duplicate();
    }

    public void ReenterTree()
    {
        if (!IsInsideTree()) return;
        var parent = GetParent();
        if (parent == null) return;
        int index = GetIndex();
        parent.RemoveChild(this);
        parent.AddChild(this);
        parent.MoveChild(this, index);
        _reentryQueued = false;
    }

    [GdfRpc(CallLocal = false, Mode = MultiplayerApi.RpcMode.AnyPeer)]
    private void ResyncRequested()
    {
        if (_reentryQueued) return;
        CallDeferred(MethodName.ReenterTree);
        _reentryQueued = true;
    }

    public override void _EnterTree()
    {
        if (!IsMultiplayerAuthority() && FixLateEntrySyncing)
            this.GdfRpcId(GetMultiplayerAuthority(), MethodName.ResyncRequested);
    }
}