using System.Collections.Generic;
using GDF.Util;
using Godot;
using Godot.Collections;

namespace GDF.PropertyStacks;

[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/property_stack_watcher.png")]
public partial class PropertyStackWatcher : Node
{
    [Signal]
    public delegate void PropertyChangedEventHandler(string propertyId, Variant prevValue, Variant newValue);
    
    /// <summary>
    /// The stack whose stack properties are to be tracked
    /// </summary>
    [Export] public PropertyStack Stack;

    [Export] public WatcherUpdateMode UpdateMode = WatcherUpdateMode.IdleProcess;
    
    /// <summary>
    /// The mappings of stack property id => node path:property to set 
    /// </summary>
    [Export] public Godot.Collections.Dictionary<string, NodePath> Mappings;

    [ExportGroup("Special Mappings")]
    [Export] public string TimeScalePropertyId;
    [Export] public string PausedPropertyId;
    [Export] public string MouseModePropertyId;
    [Export] public string CurrentCamera2DPropertyId;
    [Export] public string CurrentCamera3DPropertyId;
    
    [ExportGroup("Network","Network")]
    [Export] public Array<string> NetworkSyncedProperties;
    [Export] public int NetworkChannel = GdfConstants.DefaultRpcTransferChannel;
    
    [ExportGroup("Advanced")]
    [Export] public Array<string> AutoObservedProperties;
    
    private System.Collections.Generic.Dictionary<string, WatchedPropertyState> _prevObservedStates;
    PropertyFrame _networkSyncedFrame;
    private int? _networkSyncedAuthority;
    private List<string> _observedPropertyIds = new();

    public override void _Ready()
    {
        _prevObservedStates = new System.Collections.Generic.Dictionary<string, WatchedPropertyState>();
        
        if(Mappings != null)
            _observedPropertyIds.AddRange(Mappings.Keys);
        
        if(TimeScalePropertyId is {Length: > 0}) StartObserving(TimeScalePropertyId);
        if(PausedPropertyId is {Length: > 0}) StartObserving(PausedPropertyId);
        if(MouseModePropertyId is {Length: > 0}) StartObserving(MouseModePropertyId);
        if(CurrentCamera2DPropertyId is {Length: > 0}) StartObserving(CurrentCamera2DPropertyId);
        if(CurrentCamera3DPropertyId is {Length: > 0}) StartObserving(CurrentCamera3DPropertyId);
        if (NetworkSyncedProperties is { Count: > 0 })
            foreach(string propertyId in NetworkSyncedProperties) StartObserving(propertyId);
        if (AutoObservedProperties is { Count: > 0 })
            foreach(string propertyId in AutoObservedProperties) StartObserving(propertyId);
        // this.Resync();
    }

    public override void _Process(double delta)
    {
        if (UpdateMode == WatcherUpdateMode.IdleProcess) Observe();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (UpdateMode == WatcherUpdateMode.PhysicsProcess) Observe();
    }
    
    public void Observe()
    {
        if (Stack == null)
        {
            // Stack changed or lost
            _prevObservedStates.Clear();
            _networkSyncedFrame = _networkSyncedFrame?.Remove();
            return;
        }

        CheckNetworkChanged();
        foreach (string propertyId in _observedPropertyIds)
        {
            Variant currentValue;
            int currentModCount = Stack.GetModCount(propertyId);
            if (_prevObservedStates.TryGetValue(propertyId, out var prevState))
            {
                if (currentModCount != prevState.ModCount)
                {
                    // mod count changed, get current value (could be the same)
                    currentValue = Stack.GetEffectiveValue(propertyId);
                    if (!prevState.Value.VariantEquals(currentValue))
                    {
                        FirePropertyChanged(propertyId, prevState.Value, currentValue);
                    }
                }
                else
                {
                    currentValue = prevState.Value;
                }
            }
            else
            {
                // First time observed this property, fire changed
                currentValue = Stack.GetEffectiveValue(propertyId);
                FirePropertyChanged(propertyId, currentValue, currentValue);
            }

            _prevObservedStates[propertyId] = new WatchedPropertyState()
            {
                Value = currentValue,
                ModCount = currentModCount
            };
        }
    }

    private void CheckNetworkChanged()
    {
        if (Stack != null && _networkSyncedFrame != null && (Stack.IsMultiplayerAuthority() ||
                                                             _networkSyncedAuthority !=
                                                             Stack.GetMultiplayerAuthority()))
        {
            // Multiplayer Peer changed
            _networkSyncedAuthority = null;
            _prevObservedStates.Clear();
            _networkSyncedFrame = _networkSyncedFrame?.Remove();
        }
    }

    private void FirePropertyChanged(string propertyId, Variant prevValue, Variant newValue)
    {
        // GD.Print($"[{Name}] Property changed: {propertyId} from {prevValue} to {newValue}");
        EmitSignal(SignalName.PropertyChanged, propertyId, prevValue, newValue);

        if (propertyId == TimeScalePropertyId)
        {
            Engine.TimeScale = newValue.AsSingle();
        }
        if (propertyId == PausedPropertyId)
        {
            GetTree().Paused = newValue.AsBool();
        }
        if (propertyId == MouseModePropertyId)
        {
            Godot.Input.MouseMode = newValue.As<Godot.Input.MouseModeEnum>();
        }
        if (propertyId == CurrentCamera2DPropertyId)
        {
            newValue.As<Camera2D>()?.MakeCurrent();
        }
        if (propertyId == CurrentCamera3DPropertyId)
        {
            newValue.As<Camera3D>()?.MakeCurrent();
            prevValue.As<Camera3D>()?.ClearCurrent();
        }

        if (Mappings != null && Mappings.TryGetValue(propertyId, out var path))
        {
            var pointedNode = GetNode(path);
            
            var pathAsString = path.ToString();
            string propertyPath = pathAsString.Substring(pathAsString.IndexOf(':') + 1);
            if (propertyPath.Length != pathAsString.Length)
            {
                pointedNode.SetIndexed(propertyPath, newValue);
            }
        }

        // // TODO move out from plugin
        // if (NetworkSyncedProperties != null && NetworkSyncedProperties.Contains(propertyId) && Stack.IsMultiplayerAuthority())
        // {
        //     CustomRpc.ForChannel(NetworkChannel).SendOthers(this, Util.PropertyStacks.PropertyStackWatcher.MethodName.SyncProperty, propertyId, newValue);
        // }
    }

    // // TODO move out from plugin
    // [CustomRpc]
    // private void SyncProperty(string propertyId, Variant newValue)
    // {
    //     CheckNetworkChanged();
    //     _networkSyncedFrame ??= Stack.NewFrame("Network Synced", 999).BindToNode(this);
    //     _networkSyncedFrame.Set(propertyId, newValue);
    //     _networkSyncedAuthority ??= Stack.GetMultiplayerAuthority();
    // }

    public bool IsObserving(string propertyId)
    {
        return _observedPropertyIds.Contains(propertyId);
    }

    public void StartObserving(string propertyId)
    {
        if (IsObserving(propertyId)) return;
        _observedPropertyIds.Add(propertyId);
    }

    public void StopObserving(string propertyId)
    {
        if (!IsObserving(propertyId)) return;
        _observedPropertyIds.Remove(propertyId);
        _prevObservedStates.Remove(propertyId);
    }
    //
    // [CustomRpc]
    // public void Resync(int peerId)
    // {
    //     if (NetworkSyncedProperties is not { Count: > 0 }) return;
    //     var rpc = CustomRpc.ForChannel(NetworkChannel);
    //     foreach (string propertyId in NetworkSyncedProperties)
    //     {
    //         rpc.SendOthers(this, Util.PropertyStacks.PropertyStackWatcher.MethodName.SyncProperty, propertyId, Stack.GetEffectiveValue(propertyId));
    //     }
    // }

    private struct WatchedPropertyState
    {
        public int ModCount;
        public Variant Value;
    }
}

public enum WatcherUpdateMode
{
    IdleProcess,
    PhysicsProcess,
    Manual
}