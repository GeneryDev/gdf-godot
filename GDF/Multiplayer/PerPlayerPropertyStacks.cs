using System.Collections.Generic;
using GDF.PropertyStacks;
using GDF.PropertyStacks.Definitions.Specialized;
using GDF.PropertyStacks.Extensions;
using GDF.Util;
using Godot;
using Systems.Inputs;

namespace GDF.Multiplayer;

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
        NonModalFrame ??= PropertyStack.GlobalInstance.NewFrame("Non-modal Control", NonModalFrameOrder)
                .Set(InputGroups.Default, InputGroupMode.Capture)
                .Set(InputGroups.Mouse, InputGroupMode.PassThrough)
                .BindToNode(this)
            ;
    }

    /// <summary>
    /// Returns a dedicated static property stack for the given player ID.
    /// If the given playerId is -1, the Global PropertyStack is returned instead.
    /// </summary>
    public static PropertyStack GetForPlayer(int playerId)
    {
        if (playerId == -1) return PropertyStack.GlobalInstance;
        if (Instance == null) return null;
        if (Instance._perPlayerStacks.TryGetValue(playerId, out var existing)) return existing;

        var stack = new PropertyStack() { Name = $"Player Property Stack [{playerId}]" };
        Instance._perPlayerStacks[playerId] = stack;
        stack.PropertyRegistry = Instance.PerPlayerProperties;
        Instance.AddChild(stack);
        Instance._perPlayerBaseControlFrames[playerId] = stack.NewFrame("Base Control", -99)
                .Set(InputGroups.Default, InputGroupMode.Capture)
                .Set(InputGroups.Mouse, InputGroupMode.PassThrough)
            ;

        UpdatePlayerModalFrame(playerId);

        var watcher = new PropertyStackWatcher() { Name = "Watcher", Stack = stack };
        watcher.StartObserving(InputGroups.Default);
        watcher.StartObserving(InputGroups.Mouse);
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
        if (!playerInfo.OwnedByThisClient) return;

        var playerStack = GetForPlayer(playerId);

        PropertyFrame playerModalFrame;
        if (!Instance._perPlayerModalFrames.TryGetValue(playerId, out playerModalFrame))
        {
            playerModalFrame = playerStack.NewFrame("Modal Control", 1000);
            Instance._perPlayerModalFrames[playerId] = playerModalFrame;
        }

        playerModalFrame.Set(InputGroups.Default,
            Instance.NonModalFrame.HasControl(InputGroups.Default)
                ? InputGroupMode.PassThrough
                : InputGroupMode.Capture);
        playerModalFrame.Set(InputGroups.Mouse,
            Instance.NonModalFrame.HasControl(InputGroups.Mouse) ? InputGroupMode.PassThrough : InputGroupMode.Capture);
    }

    public override void _Process(double delta)
    {
        if (!Room.InstanceExists) return;
        foreach (var player in Room.Instance.GetAllPlayerInfo()) UpdatePlayerModalFrame(player.PlayerId);
    }

    public static bool HasBaseControl(int playerId, string propertyId)
    {
        return InstanceExists && Instance._perPlayerBaseControlFrames.TryGetValue(playerId, out var playerFrame) &&
               playerFrame.HasControl(propertyId);
    }

    public static bool HasNonModalControl()
    {
        return Instance.NonModalFrame.HasControl(InputGroups.Default);
    }
}