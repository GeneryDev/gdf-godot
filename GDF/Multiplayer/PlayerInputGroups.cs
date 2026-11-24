using System.Collections.Generic;
using GDF.Attributes;
using GDF.Attributes.Impl;
using GDF.Input;
using GDF.Util;
using Godot;

namespace GDF.Multiplayer;

[GlobalClass]
[SingletonUsage(SingletonUsage.Autoload)]
public partial class PlayerInputGroups : SingletonNode<PlayerInputGroups>
{
    [Signal]
    public delegate void PlayerInputStackChangedEventHandler();

    public const int NonModalOrder = -1;
    public const int BaseControlOrder = -99;
    public const int ModalOrder = 1000;
    
    public static readonly InputGroups Global = new();
    
    private static readonly BulkModifier NonModal = new("Non-modal Control", NonModalOrder);
    private static readonly Dictionary<int, PlayerData> PlayerDataById = new();
    private AttributeWatcher _watcher;

    public override void _Ready()
    {
        base._Ready();
        InitializeNonModal();
        InitializeWatcher();
    }

    private void InitializeWatcher()
    {
        if (_watcher != null) return;
        _watcher = new AttributeWatcher();
        _watcher.Updated += EmitSignalPlayerInputStackChanged;
        this.AddChild(_watcher);
    }

    private static void InitializeNonModal()
    {
        foreach (var inputStack in Global.GetAll().Values)
            NonModal.Modify(inputStack, InputGroupMode.PassThrough);
    }

    private static PlayerData GetOrCreatePlayerData(int playerId)
    {
        if (playerId == -1) return default;
        if (PlayerDataById.TryGetValue(playerId, out var existing)) return existing;

        var playerGroups = new InputGroups();

        var baseControl = new BulkModifier("Base Control", BaseControlOrder);
        
        foreach ((string groupName, var inputStack) in playerGroups.GetAll())
        {
            baseControl.Modify(inputStack, InputGroupMode.PassThrough);
            Instance._watcher.StartObserving($"player_{playerId}_{groupName}", inputStack);
        }
        
        var playerData = PlayerDataById[playerId] = new PlayerData()
        {
            Groups = playerGroups,
            BaseControl = baseControl,
            Modal = new BulkModifier("Modal Control", ModalOrder)
        };

        UpdatePlayerModal(playerId);

        return playerData;
    }

    public static InputGroups GetForPlayer(int playerId)
    {
        return GetOrCreatePlayerData(playerId).Groups;
    }

    private static void UpdatePlayerModal(int playerId)
    {
        var playerInfo = Room.Instance.GetPlayerInfo(playerId) ?? default;
        if (!playerInfo.IsLocal) return;

        var playerData = GetOrCreatePlayerData(playerId);

        var modalModifier = playerData.Modal;

        foreach ((string name, var inputStack) in playerData.Groups.GetAll())
        {
            modalModifier.Modify(inputStack,
                NonModal.HasControl(Global.Get(name))
                    ? InputGroupMode.PassThrough
                    : InputGroupMode.Capture);
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (!Room.InstanceExists) return;
        foreach (var playerInfo in Room.Instance.GetAllPlayerInfo())
        {
            UpdatePlayerModal(playerInfo.PlayerId);
        }
    }

    public static bool HasBaseControl(int playerId, string groupName)
    {
        return GetOrCreatePlayerData(playerId).HasBaseControl(groupName);
    }

    public static bool HasNonModalControl(string groupName)
    {
        return NonModal.HasControl(Global.Get(groupName));
    }

    private struct PlayerData
    {
        public InputGroups Groups;
        public BulkModifier BaseControl;
        public BulkModifier Modal;

        public bool HasBaseControl(string groupName)
        {
            return BaseControl?.HasControl(Groups.Get(groupName)) ?? false;
        }
    }
}