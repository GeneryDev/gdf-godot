using System;
using GDF.Data;
using GDF.Logical;
using GDF.Networking;
using Godot;
using Godot.Collections;

namespace GDF.Multiplayer;

[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/fraction.png")]
public partial class PlayerAwaiter : Node, IResynchronizable, IDataContext
{
    public const int PlayerJoinBehaviorNone = 0;
    public const int PlayerLeaveBehaviorNone = 0;

    public const int PlayerJoinBehaviorAddToWaitList = 1;
    public const int PlayerLeaveBehaviorRemoveFromWaitList = 1;

    public const int PlayerJoinBehaviorAddAndConfirm = 2;
    public const int PlayerLeaveBehaviorConfirm = 2;

    [Signal]
    public delegate void StartedEventHandler();

    [Signal]
    public delegate void UpdatedEventHandler();

    [Signal]
    public delegate void CompletedEventHandler();

    [Signal]
    public delegate void RetractedCompletionEventHandler();

    [Export] public bool Autostart = false;
    [Export] public PlayerAwaitMode Mode = PlayerAwaitMode.AllPlayers;
    [Export] public bool OnlyAwaitLocalPlayers = false;
    [Export] public bool AllowRetractingConfirmation = false;
    [Export] public bool StopAwaitingOnCompleted = true;
    [Export] public bool RequireAtLeastOnePlayer = true;

    [ExportGroup("Player List Update Behaviors")]
    [Export(PropertyHint.Enum, "None,Add to Wait List,Add and Confirm")]
    public int PlayerJoinBehavior = PlayerJoinBehaviorNone;

    [Export(PropertyHint.Enum, "None,Remove from Wait List,Confirm")]
    public int PlayerLeaveBehavior = PlayerLeaveBehaviorRemoveFromWaitList;

    [ExportGroup("Networking")]
    [Export] public AuthorityMode ManagingAuthorityMode = AuthorityMode.Authority;
    [Export] public AuthorityMode ConfirmationAuthorityMode = AuthorityMode.AnyPeer;
    [Export] public bool ReplicateToPeers = true;
    [Export] public bool RestrictConfirmationMethodsToOwnedPlayers = false;

    public bool Awaiting { get; private set; }
    private Array<int> _allAwaitingPlayers = new();
    private Array<int> _confirmedPlayers = new();
    public bool IsCompleted { get; private set; }
    public int TotalAwaiting => _allAwaitingPlayers.Count;
    public int ConfirmedCount => _confirmedPlayers.Count;
    public int RemainingCount => TotalAwaiting - ConfirmedCount;

    public void Start()
    {
        if (!ManagingAuthorityMode.CanExecute(this)) return;

        _allAwaitingPlayers = CollectPlayerIdsToAwait(_allAwaitingPlayers);

        if (ReplicateToPeers)
            Rpc(MethodName.StartAwaitingRpc, _allAwaitingPlayers);
        else
            StartAwaitingRpc(_allAwaitingPlayers);

        CheckCompleted();
    }

    private Array<int> CollectPlayerIdsToAwait(Array<int> ids)
    {
        ids.Clear();

        foreach (var playerInfo in Room.Instance.GetAllPlayerInfo())
        {
            if (OnlyAwaitLocalPlayers && !playerInfo.OwnedByThisClient) continue;
            ids.Add(playerInfo.PlayerId);
        }

        return ids;
    }

    private void PrintConfigurationCombinationWarnings()
    {
        if (OnlyAwaitLocalPlayers && ReplicateToPeers)
            GD.PushWarning(
                $"Player Awaiter started with {nameof(OnlyAwaitLocalPlayers)}={OnlyAwaitLocalPlayers} and {nameof(ReplicateToPeers)}={ReplicateToPeers}. This combination will likely cause unintended behavior.\nAt {(IsInsideTree() ? GetPath() : "(orphan node)")}");
        if (ManagingAuthorityMode != AuthorityMode.Authority && ReplicateToPeers)
            GD.PushWarning(
                $"Player Awaiter started with {nameof(ManagingAuthorityMode)}={ManagingAuthorityMode} and {nameof(ReplicateToPeers)}={ReplicateToPeers}. This combination will likely cause unintended behavior.\nAt {(IsInsideTree() ? GetPath() : "(orphan node)")}");
    }

    [Rpc(MultiplayerApi.RpcMode.Authority,
        CallLocal = true,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,
        TransferChannel = GdfConstants.DefaultRpcTransferChannel)]
    private void StartAwaitingRpc(Array<int> playerIds)
    {
        // GD.Print("[PlayerAwaiter] Now awaiting");
        Awaiting = true;
        IsCompleted = false;
        PrintConfigurationCombinationWarnings();
        _allAwaitingPlayers = playerIds;
        _confirmedPlayers.Clear();
        EmitSignal(SignalName.Started);
        EmitSignal(SignalName.Updated);
    }

    public void ConfirmPlayer(int playerId)
    {
        if (!ConfirmationAuthorityMode.CanExecute(this)) return;

        if (RestrictConfirmationMethodsToOwnedPlayers &&
            !(Room.Instance.TryGetPlayerInfo(playerId, out var playerInfo) &&
              playerInfo.OwnedByThisClient)) return;

        if (ReplicateToPeers)
            RpcId(GetMultiplayerAuthority(), MethodName.ManageConfirmPlayerRpc, playerId);
        else
            ManageConfirmPlayerRpc(playerId);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer,
        CallLocal = true,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,
        TransferChannel = GdfConstants.DefaultRpcTransferChannel)]
    private void ManageConfirmPlayerRpc(int playerId)
    {
        if (!ManagingAuthorityMode.CanExecute(this)) return;

        if (!Awaiting) return; // not waiting
        if (!_allAwaitingPlayers.Contains(playerId)) return; // not waiting for this player
        if (_confirmedPlayers.Contains(playerId)) return; // already confirmed

        if (ReplicateToPeers)
            Rpc(MethodName.NotifyConfirmedPlayerRpc, playerId);
        else
            NotifyConfirmedPlayerRpc(playerId);

        CheckCompleted();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer,
        CallLocal = true,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,
        TransferChannel = GdfConstants.DefaultRpcTransferChannel)]
    private void NotifyConfirmedPlayerRpc(int playerId)
    {
        if (!Awaiting) return;

        if (_confirmedPlayers.Contains(playerId)) return;
        _confirmedPlayers.Add(playerId);
        // GD.Print($"[PlayerAwaiter] Confirmed {playerId}");
        EmitSignal(SignalName.Updated);
    }

    public void RetractPlayerConfirmation(int playerId)
    {
        if (!ConfirmationAuthorityMode.CanExecute(this)) return;

        if (RestrictConfirmationMethodsToOwnedPlayers &&
            !(Room.Instance.TryGetPlayerInfo(playerId, out var playerInfo) &&
              playerInfo.OwnedByThisClient)) return;

        if (!AllowRetractingConfirmation) return;

        if (ReplicateToPeers)
            RpcId(GetMultiplayerAuthority(), MethodName.ManageRetractPlayerConfirmationRpc, playerId);
        else
            ManageRetractPlayerConfirmationRpc(playerId);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer,
        CallLocal = true,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,
        TransferChannel = GdfConstants.DefaultRpcTransferChannel)]
    private void ManageRetractPlayerConfirmationRpc(int playerId)
    {
        if (!ManagingAuthorityMode.CanExecute(this)) return;

        if (!Awaiting) return; // not waiting
        if (!_allAwaitingPlayers.Contains(playerId)) return; // not waiting for this player
        if (!_confirmedPlayers.Contains(playerId)) return; // not confirmed in the first place

        if (ReplicateToPeers)
            Rpc(MethodName.NotifyRetractedPlayerConfirmationRpc, playerId);
        else
            NotifyRetractedPlayerConfirmationRpc(playerId);

        CheckCompleted();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer,
        CallLocal = true,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,
        TransferChannel = GdfConstants.DefaultRpcTransferChannel)]
    private void NotifyRetractedPlayerConfirmationRpc(int playerId)
    {
        if (!Awaiting) return;

        if (!_confirmedPlayers.Contains(playerId)) return;
        _confirmedPlayers.Remove(playerId);
        // GD.Print($"[PlayerAwaiter] Confirmed {playerId}");
        EmitSignal(SignalName.Updated);
    }


    private void ManageAddPlayer(int playerId)
    {
        if (!ManagingAuthorityMode.CanExecute(this)) return;

        if (!Awaiting) return; // not waiting
        if (_allAwaitingPlayers.Contains(playerId)) return; // already waiting for this player

        if (ReplicateToPeers)
            Rpc(MethodName.NotifyPlayerAddedRpc, playerId);
        else
            NotifyPlayerAddedRpc(playerId);

        CheckCompleted();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer,
        CallLocal = true,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,
        TransferChannel = GdfConstants.DefaultRpcTransferChannel)]
    private void NotifyPlayerAddedRpc(int playerId)
    {
        if (!Awaiting) return;

        if (_allAwaitingPlayers.Contains(playerId)) return;
        _allAwaitingPlayers.Add(playerId);
        // GD.Print($"[PlayerAwaiter] Confirmed {playerId}");
        EmitSignal(SignalName.Updated);
    }


    private void ManageRemovePlayer(int playerId)
    {
        if (!ManagingAuthorityMode.CanExecute(this)) return;

        if (!Awaiting) return; // not waiting
        if (!_allAwaitingPlayers.Contains(playerId)) return; // not waiting for this player in the first place

        if (ReplicateToPeers)
            Rpc(MethodName.NotifyPlayerRemovedRpc, playerId);
        else
            NotifyPlayerRemovedRpc(playerId);

        CheckCompleted();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer,
        CallLocal = true,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,
        TransferChannel = GdfConstants.DefaultRpcTransferChannel)]
    private void NotifyPlayerRemovedRpc(int playerId)
    {
        if (!Awaiting) return;

        if (!_allAwaitingPlayers.Contains(playerId)) return;
        _allAwaitingPlayers.Remove(playerId);
        _confirmedPlayers.Remove(playerId);
        // GD.Print($"[PlayerAwaiter] Confirmed {playerId}");
        EmitSignal(SignalName.Updated);
    }

    private void CheckCompleted()
    {
        if (ManagingAuthorityMode.CanExecute(this) && IsCompleted != ConditionMetAfterUpdate())
        {
            if (!IsCompleted)
            {
                if (ReplicateToPeers)
                    Rpc(MethodName.MarkCompletedRpc);
                else
                    MarkCompletedRpc();
            }
            else
            {
                if (ReplicateToPeers)
                    Rpc(MethodName.MarkUncompletedRpc);
                else
                    MarkUncompletedRpc();
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority,
        CallLocal = true,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,
        TransferChannel = GdfConstants.DefaultRpcTransferChannel)]
    private void MarkCompletedRpc()
    {
        // GD.Print($"[PlayerAwaiter] Completed");
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
        // GD.Print($"[PlayerAwaiter] Uncompleted");
        IsCompleted = false;
        EmitSignal(SignalName.RetractedCompletion);
        EmitSignal(SignalName.Updated);
    }

    private bool ConditionMetAfterUpdate()
    {
        if (RequireAtLeastOnePlayer && ConfirmedCount == 0) return false;
        return Mode switch
        {
            PlayerAwaitMode.AnyPlayer => ConfirmedCount > 0,
            PlayerAwaitMode.AllPlayers => RemainingCount == 0,
            PlayerAwaitMode.Majority => ConfirmedCount >= TotalAwaiting / 2,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private void OnPlayerJoined(int playerId)
    {
        if (!ManagingAuthorityMode.CanExecute(this)) return;
        if (OnlyAwaitLocalPlayers && Room.Instance.TryGetPlayerInfo(playerId, out var playerInfo) &&
            !playerInfo.OwnedByThisClient) return;

        switch (PlayerJoinBehavior)
        {
            case PlayerJoinBehaviorNone:
                return;
            case PlayerJoinBehaviorAddToWaitList:
                ManageAddPlayer(playerId);
                return;
            case PlayerJoinBehaviorAddAndConfirm:
                ManageAddPlayer(playerId);
                ManageConfirmPlayerRpc(playerId);
                return;
        }
    }

    private void OnPlayerLeft(int playerId)
    {
        if (!ManagingAuthorityMode.CanExecute(this)) return;

        switch (PlayerLeaveBehavior)
        {
            case PlayerLeaveBehaviorNone:
                return;
            case PlayerLeaveBehaviorRemoveFromWaitList:
                ManageRemovePlayer(playerId);
                return;
            case PlayerLeaveBehaviorConfirm:
                ManageConfirmPlayerRpc(playerId);
                return;
        }
    }

    public override void _EnterTree()
    {
        Room.Instance.PlayerJoined += OnPlayerJoined;
        Room.Instance.PlayerLeft += OnPlayerLeft;
    }

    public override void _ExitTree()
    {
        Room.Instance.PlayerJoined -= OnPlayerJoined;
        Room.Instance.PlayerLeft -= OnPlayerLeft;
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
        if (!ReplicateToPeers) return;
        if (Awaiting)
        {
            RpcId(peerId, MethodName.StartAwaitingRpc, _allAwaitingPlayers);
            foreach (int playerId in _confirmedPlayers) RpcId(peerId, MethodName.NotifyConfirmedPlayerRpc, playerId);

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

    public PlayerAwaitStatus GetPlayerStatus(int playerId)
    {
        if (!_allAwaitingPlayers.Contains(playerId)) return PlayerAwaitStatus.NotWaiting;
        if (_confirmedPlayers.Contains(playerId)) return PlayerAwaitStatus.Confirmed;
        return PlayerAwaitStatus.Waiting;
    }

    public enum PlayerAwaitMode
    {
        AnyPlayer,
        Majority,
        AllPlayers
    }

    public enum PlayerAwaitStatus
    {
        NotWaiting,
        Waiting,
        Confirmed
    }
}