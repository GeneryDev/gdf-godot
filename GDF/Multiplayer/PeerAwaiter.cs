using System;
using GDF.Data;
using GDF.Logical;
using GDF.Networking;
using Godot;
using Godot.Collections;

namespace GDF.Multiplayer;

[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/fraction.png")]
public partial class PeerAwaiter : Node, IResynchronizable, IDataContext
{
    public const int PeerConnectBehaviorNone = 0;
    public const int PeerDisconnectBehaviorNone = 0;

    public const int PeerConnectBehaviorAddToWaitList = 1;
    public const int PeerDisconnectBehaviorRemoveFromWaitList = 1;

    public const int PeerConnectBehaviorAddAndConfirm = 2;
    public const int PeerDisconnectBehaviorConfirm = 2;

    [Signal]
    public delegate void StartedEventHandler();

    [Signal]
    public delegate void UpdatedEventHandler();

    [Signal]
    public delegate void CompletedEventHandler();

    [Signal]
    public delegate void RetractedCompletionEventHandler();

    [Export] public bool Autostart = false;
    [Export] public PeerAwaitMode Mode = PeerAwaitMode.AllPeers;
    [Export] public bool AllowConfirmationRetracting = false;
    [Export] public bool StopAwaitingOnCompleted = true;

    [ExportGroup("Peer Update Behaviors")]
    [Export(PropertyHint.Enum, "None,Add to Wait List,Add and Confirm")]
    public int PeerConnectBehavior = PeerConnectBehaviorNone;

    [Export(PropertyHint.Enum, "None,Remove from Wait List,Confirm")]
    public int PeerDisconnectBehavior = PeerDisconnectBehaviorRemoveFromWaitList;

    [ExportGroup("Networking")]
    [Export] public AuthorityMode ManagingAuthorityMode = AuthorityMode.Authority;

    public bool Awaiting { get; private set; }
    private Array<int> _allAwaitingPeers = new();
    private Array<int> _confirmedPeers = new();
    public bool IsCompleted { get; private set; }
    public int TotalAwaiting => _allAwaitingPeers.Count;
    public int ConfirmedCount => _confirmedPeers.Count;
    public int RemainingCount => TotalAwaiting - ConfirmedCount;

    public void Start()
    {
        if (!ManagingAuthorityMode.CanExecute(this)) return;

        _allAwaitingPeers = CollectPeerIdsToAwait(_allAwaitingPeers);

        Rpc(MethodName.StartAwaitingRpc, _allAwaitingPeers);

        CheckCompleted();
    }

    private Array<int> CollectPeerIdsToAwait(Array<int> ids)
    {
        ids.Clear();

        if (Room.InstanceExists)
        {
            foreach (int peerId in Room.Instance.GetAllPeerIds()) ids.Add(peerId);
        }
        else
        {
            ids.Add(GetMultiplayerAuthority());
        }

        return ids;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority,
        CallLocal = true,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,
        TransferChannel = GdfConstants.DefaultRpcTransferChannel)]
    private void StartAwaitingRpc(Array<int> peerIds)
    {
        Awaiting = true;
        IsCompleted = false;
        _allAwaitingPeers = peerIds;
        _confirmedPeers.Clear();
        EmitSignal(SignalName.Started);
        EmitSignal(SignalName.Updated);
    }

    public void ConfirmSelf()
    {
        RpcId(GetMultiplayerAuthority(), MethodName.ManageConfirmPeerRpc);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer,
        CallLocal = true,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,
        TransferChannel = GdfConstants.DefaultRpcTransferChannel)]
    private void ManageConfirmPeerRpc()
    {
        ManageConfirmPeerRpc(Multiplayer.GetRemoteSenderId());
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer,
        CallLocal = true,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,
        TransferChannel = GdfConstants.DefaultRpcTransferChannel)]
    private void ManageConfirmPeerRpc(int peerId)
    {
        if (!ManagingAuthorityMode.CanExecute(this)) return;

        if (!Awaiting) return; // not waiting
        if (!_allAwaitingPeers.Contains(peerId)) return; // not waiting for this peer
        if (_confirmedPeers.Contains(peerId)) return; // already confirmed

        Rpc(MethodName.NotifyConfirmedPeerRpc, peerId);

        CheckCompleted();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer,
        CallLocal = true,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,
        TransferChannel = GdfConstants.DefaultRpcTransferChannel)]
    private void NotifyConfirmedPeerRpc(int peerId)
    {
        if (!Awaiting) return;

        if (_confirmedPeers.Contains(peerId)) return;
        _confirmedPeers.Add(peerId);
        // GD.Print($"[PeerAwaiter] Confirmed {peerId}");
        EmitSignal(SignalName.Updated);
    }

    public void RetractSelfConfirmation()
    {
        if (!AllowConfirmationRetracting) return;
        RpcId(GetMultiplayerAuthority(), MethodName.ManageRetractPeerConfirmationRpc);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer,
        CallLocal = true,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,
        TransferChannel = GdfConstants.DefaultRpcTransferChannel)]
    private void ManageRetractPeerConfirmationRpc()
    {
        if (!ManagingAuthorityMode.CanExecute(this)) return;

        if (!Awaiting) return; // not waiting
        int peerId = Multiplayer.GetRemoteSenderId();
        if (!_allAwaitingPeers.Contains(peerId)) return; // not waiting for this peer
        if (!_confirmedPeers.Contains(peerId)) return; // not confirmed in the first place

        Rpc(MethodName.NotifyRetractedPeerConfirmationRpc, peerId);

        CheckCompleted();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer,
        CallLocal = true,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,
        TransferChannel = GdfConstants.DefaultRpcTransferChannel)]
    private void NotifyRetractedPeerConfirmationRpc(int peerId)
    {
        if (!Awaiting) return;

        if (!_confirmedPeers.Contains(peerId)) return;
        _confirmedPeers.Remove(peerId);
        // GD.Print($"[PeerAwaiter] Unconfirmed {peerId}");
        EmitSignal(SignalName.Updated);
    }


    private void ManageAddPeer(int peerId)
    {
        if (!ManagingAuthorityMode.CanExecute(this)) return;

        if (!Awaiting) return; // not waiting
        if (_allAwaitingPeers.Contains(peerId)) return; // already waiting for this peer

        Rpc(MethodName.NotifyPeerAddedRpc, peerId);

        CheckCompleted();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer,
        CallLocal = true,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,
        TransferChannel = GdfConstants.DefaultRpcTransferChannel)]
    private void NotifyPeerAddedRpc(int peerId)
    {
        if (!Awaiting) return;

        if (_allAwaitingPeers.Contains(peerId)) return;
        _allAwaitingPeers.Add(peerId);
        // GD.Print($"[PeerAwaiter] Added {peerId}");
        EmitSignal(SignalName.Updated);
    }


    private void ManageRemovePeer(int peerId)
    {
        if (!ManagingAuthorityMode.CanExecute(this)) return;

        if (!Awaiting) return; // not waiting
        if (!_allAwaitingPeers.Contains(peerId)) return; // not waiting for this peer in the first place

        Rpc(MethodName.NotifyPeerRemovedRpc, peerId);

        CheckCompleted();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer,
        CallLocal = true,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,
        TransferChannel = GdfConstants.DefaultRpcTransferChannel)]
    private void NotifyPeerRemovedRpc(int peerId)
    {
        if (!Awaiting) return;

        if (!_allAwaitingPeers.Contains(peerId)) return;
        _allAwaitingPeers.Remove(peerId);
        _confirmedPeers.Remove(peerId);
        // GD.Print($"[PeerAwaiter] Removed {peerId}");
        EmitSignal(SignalName.Updated);
    }

    private void CheckCompleted()
    {
        if (ManagingAuthorityMode.CanExecute(this) && IsCompleted != ConditionMetAfterUpdate())
        {
            if (!IsCompleted)
                Rpc(MethodName.MarkCompletedRpc);
            else
                Rpc(MethodName.MarkUncompletedRpc);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority,
        CallLocal = true,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,
        TransferChannel = GdfConstants.DefaultRpcTransferChannel)]
    private void MarkCompletedRpc()
    {
        // GD.Print($"[PeerAwaiter] Completed");
        IsCompleted = true;
        if (StopAwaitingOnCompleted)
            Awaiting = false;
        EmitSignal(SignalName.Completed);
        EmitSignal(SignalName.Updated);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority,
        CallLocal = true,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,
        TransferChannel = GdfConstants.DefaultRpcTransferChannel)]
    private void MarkUncompletedRpc()
    {
        // GD.Print($"[PeerAwaiter] Uncompleted");
        IsCompleted = false;
        EmitSignal(SignalName.RetractedCompletion);
        EmitSignal(SignalName.Updated);
    }

    private bool ConditionMetAfterUpdate()
    {
        return Mode switch
        {
            PeerAwaitMode.AnyPeer => ConfirmedCount > 0,
            PeerAwaitMode.AllPeers => RemainingCount == 0,
            PeerAwaitMode.Majority => ConfirmedCount >= TotalAwaiting / 2,
            PeerAwaitMode.Host => !_confirmedPeers.Contains(1),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private void OnPeerConnected(int peerId)
    {
        if (!ManagingAuthorityMode.CanExecute(this)) return;

        switch (PeerConnectBehavior)
        {
            case PeerConnectBehaviorNone:
                return;
            case PeerConnectBehaviorAddToWaitList:
                ManageAddPeer(peerId);
                return;
            case PeerConnectBehaviorAddAndConfirm:
                ManageAddPeer(peerId);
                ManageConfirmPeerRpc(peerId);
                return;
        }
    }

    private void OnPeerDisconnected(int peerId)
    {
        if (!ManagingAuthorityMode.CanExecute(this)) return;

        switch (PeerDisconnectBehavior)
        {
            case PeerDisconnectBehaviorNone:
                return;
            case PeerDisconnectBehaviorRemoveFromWaitList:
                ManageRemovePeer(peerId);
                return;
            case PeerDisconnectBehaviorConfirm:
                ManageConfirmPeerRpc(peerId);
                return;
        }
    }

    public override void _EnterTree()
    {
        if (Room.InstanceExists)
        {
            Room.Instance.PeerConnected += OnPeerConnected;
            Room.Instance.PeerDisconnected += OnPeerDisconnected;
        }
    }

    public override void _ExitTree()
    {
        if (Room.InstanceExists)
        {
            Room.Instance.PeerConnected -= OnPeerConnected;
            Room.Instance.PeerDisconnected -= OnPeerDisconnected;
        }
    }

    public override void _Ready()
    {
        base._Ready();
        this.RequestResync();
        if (Autostart) Start();
    }

    [GdfRpc]
    public void Resync(int peerId)
    {
        if (Awaiting)
        {
            RpcId(peerId, MethodName.StartAwaitingRpc, _allAwaitingPeers);
            foreach (int confirmedId in _confirmedPeers) RpcId(peerId, MethodName.NotifyConfirmedPeerRpc, confirmedId);

            if (IsCompleted)
                RpcId(peerId, MethodName.MarkCompletedRpc);
        }
    }


    public StringName UpdatedSignalName => SignalName.Updated;

    public bool GetContextVariable(string key, string input, ref Variant output, IDataQueryOptions options)
    {
        switch (key)
        {
            case "awaiter":
            {
                output = this;
                return true;
            }
            case "waiting":
            case "awaiting":
            case "is_waiting":
            case "is_awaiting":
            {
                return this.OutputBooleanVariable(Awaiting, ref output, input);
            }
            case "completed":
            case "is_completed":
            {
                return this.OutputBooleanVariable(IsCompleted, ref output, input);
            }
            case "current":
            case "current_count":
            case "confirmed":
            case "confirmed_count":
            {
                return this.OutputIntVariable(ConfirmedCount, ref output, input);
            }
            case "remaining":
            case "remaining_count":
            {
                return this.OutputIntVariable(RemainingCount, ref output, input);
            }
            case "total":
            case "total_count":
            {
                return this.OutputIntVariable(TotalAwaiting, ref output, input);
            }
        }

        return false;
    }

    public PeerAwaitStatus GetPeerStatus(int peerId)
    {
        if (!_allAwaitingPeers.Contains(peerId)) return PeerAwaitStatus.NotWaiting;
        if (_confirmedPeers.Contains(peerId)) return PeerAwaitStatus.Confirmed;
        return PeerAwaitStatus.Waiting;
    }

    public enum PeerAwaitMode
    {
        AnyPeer,
        Majority,
        AllPeers,
        Host
    }

    public enum PeerAwaitStatus
    {
        NotWaiting,
        Waiting,
        Confirmed
    }
}