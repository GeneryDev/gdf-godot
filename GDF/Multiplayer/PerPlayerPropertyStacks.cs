using System.Collections.Generic;
using GDF.PropertyStacks;
using GDF.PropertyStacks.Definitions.Specialized;
using GDF.PropertyStacks.Extensions;
using GDF.Util;
using Godot;

namespace GDF.Multiplayer;

[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/per_player_property_stacks.png")]
public partial class PerPlayerPropertyStacks : SingletonNode<PerPlayerPropertyStacks>
{
    [Signal]
    public delegate void PlayerControlStackChangedEventHandler();
    
    [Export] public PropertyRegistry PerPlayerProperties;
    [Export] public int NonModalFrameOrder = -1;

    public PropertyFrame NonModalFrame;

    private readonly Dictionary<int, PropertyStack> _perPlayerStacks = new();
    private readonly Dictionary<int, PropertyFrame> _perPlayerBaseControlFrames = new();
    private readonly Dictionary<int, PropertyFrame> _perPlayerModalFrames = new();

    public override void _Ready()
    {
        base._Ready();
        if (NonModalFrame == null)
        {
            NonModalFrame = GlobalPropertyStack.Instance.NewFrame("Non-modal Control", NonModalFrameOrder).BindToNode(this);
            foreach (string id in InputGroups.GetAll())
            {
                NonModalFrame.Set(id, InputGroupMode.PassThrough);
            }
        }
    }

    /// <summary>
    /// Returns a dedicated static property stack for the given player ID.
    /// If the given playerId is -1, the Global PropertyStack is returned instead.
    /// </summary>
    public static PropertyStack GetForPlayer(int playerId)
    {
        if (playerId == -1) return GlobalPropertyStack.Instance;
        if (Instance == null) return null;
        if (Instance._perPlayerStacks.TryGetValue(playerId, out var existing)) return existing;

        var stack = new PropertyStack() { Name = $"Player Property Stack [{playerId}]" };
        Instance._perPlayerStacks[playerId] = stack;
        stack.PropertyRegistry = Instance.PerPlayerProperties;
        Instance.AddChild(stack);
        var baseControlFrame = Instance._perPlayerBaseControlFrames[playerId] = stack.NewFrame("Base Control", -99);
        foreach (string key in InputGroups.GetAll())
        {
            baseControlFrame.Set(key, InputGroupMode.PassThrough);
        }

        UpdatePlayerModalFrame(playerId);

        var watcher = new PropertyStackWatcher() { Name = "Watcher", Stack = stack };
        foreach (string key in InputGroups.GetAll())
        {
            watcher.StartObserving(key);
        }
        stack.AddChild(watcher);
        watcher.PropertyChanged += WatcherOnPropertyChanged;

        // TODO when players disconnect, remove their stacks

        return stack;
    }

    private static void WatcherOnPropertyChanged(string propertyId, Variant prevValue, Variant newValue)
    {
        Instance.EmitSignalPlayerControlStackChanged();
    }

    private static void UpdatePlayerModalFrame(int playerId)
    {
        if (Instance == null) return;
        var playerInfo = Room.Instance.GetPlayerInfo(playerId) ?? default;
        if (!playerInfo.IsLocal) return;

        var playerStack = GetForPlayer(playerId);

        PropertyFrame playerModalFrame;
        if (!Instance._perPlayerModalFrames.TryGetValue(playerId, out playerModalFrame))
        {
            playerModalFrame = playerStack.NewFrame("Modal Control", 1000);
            Instance._perPlayerModalFrames[playerId] = playerModalFrame;
        }
        
        
        foreach (string key in InputGroups.GetAll())
        {
            playerModalFrame.Set(key, Instance.NonModalFrame.HasControl(key)
                ? InputGroupMode.PassThrough
                : InputGroupMode.Capture);
        }
    }

    public override void _Process(double delta)
    {
        if (!Room.InstanceExists) return;
        foreach (var player in Room.Instance.GetAllPlayerInfo()) UpdatePlayerModalFrame(player.PlayerId);
    }

    public static bool HasBaseControl(int playerId, string propertyId)
    {
        GetForPlayer(playerId);
        return InstanceExists && Instance._perPlayerBaseControlFrames.TryGetValue(playerId, out var playerFrame) &&
               playerFrame.HasControl(propertyId);
    }

    public static bool HasNonModalControl(string propertyId)
    {
        return Instance.NonModalFrame.HasControl(propertyId);
    }
}