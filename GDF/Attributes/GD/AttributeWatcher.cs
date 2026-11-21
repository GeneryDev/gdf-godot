using GDF.Attributes.Impl;
using GDF.Composition;
using GDF.Networking;
using GDF.Util;
using Godot;
using Godot.Collections;

namespace GDF.Attributes;

[GlobalClass]
public partial class AttributeWatcher : Node, IResynchronizable
{
    [Signal]
    public delegate void UpdatedEventHandler();
    [Signal]
    public delegate void AttributeUpdatedEventHandler(StringName id, Variant prevValue, Variant newValue);

    [Export] public AttributeIndex AttributeIndex;

    [Export] public UpdateModeEnum UpdateMode = UpdateModeEnum.Idle;
    
    [ExportGroup("Network","Network")]
    [Export] public Array<string> NetworkSyncedAttributes = new();
    
    [ExportGroup("Advanced")]
    [Export] public Array<string> AutoObservedAttributes;
    
    private readonly System.Collections.Generic.Dictionary<string, WatchedAttributeState> _prevObservedStates = new();
    private AttributeSetModifier<StringName> _networkSyncedFrame;
    private int? _networkSyncedAuthority;
    private readonly System.Collections.Generic.Dictionary<StringName, IAttribute> _observedAttributes = new();
    
    public void Observe()
    {
        if (AttributeIndex == null)
        {
            // Index changed or lost
            _prevObservedStates.Clear();
            _networkSyncedFrame?.Clear();
            _networkSyncedFrame = null;
            return;
        }

        CheckNetworkChanged();
        foreach (var (attributeId, attribute) in _observedAttributes)
        {
            var computable = (IAttributeComputable<Variant>)attribute;
            Variant previousValue = default;
            Variant currentValue;
            
            var fireUpdated = false;
            int currentEditCount = attribute.GetEditCount();
            if (_prevObservedStates.TryGetValue(attributeId, out var prevState))
            {
                if (currentEditCount != prevState.EditCount)
                {
                    // mod count changed, get current value (could be the same)
                    currentValue = computable.CurrentValue;
                    if (!prevState.Value.VariantEquals(currentValue))
                    {
                        fireUpdated = true;
                        previousValue = prevState.Value;
                    }
                }
                else
                {
                    currentValue = prevState.Value;
                }
            }
            else
            {
                // First time observed this attribute, fire changed
                fireUpdated = true;
                currentValue = computable.CurrentValue;
                previousValue = currentValue;
            }

            _prevObservedStates[attributeId] = new WatchedAttributeState()
            {
                Value = currentValue,
                EditCount = currentEditCount
            };

            if (fireUpdated)
            {
                FireAttributeUpdated(attributeId, previousValue, currentValue);
            }
        }
    }

    private void CheckNetworkChanged()
    {
        if (AttributeIndex != null && _networkSyncedFrame != null && (AttributeIndex.IsMultiplayerAuthority() ||
                                                             _networkSyncedAuthority !=
                                                             AttributeIndex.GetMultiplayerAuthority()))
        {
            // Multiplayer Peer changed
            _networkSyncedAuthority = null;
            _prevObservedStates.Clear();
            _networkSyncedFrame?.Clear();
        }
    }

    private void FireAttributeUpdated(StringName attributeId, Variant prevValue, Variant newValue)
    {
        // GdfLogger.Error($"[{Name}] Attribute changed: {attributeId} from {prevValue} to {newValue}");
        EmitSignalUpdated();
        EmitSignalAttributeUpdated(attributeId, prevValue, newValue);

        // if (propertyId == TimeScalePropertyId)
        // {
        //     Engine.TimeScale = newValue.AsSingle();
        // }
        // if (propertyId == PausedPropertyId)
        // {
        //     GetTree().Paused = newValue.AsBool();
        // }
        // if (propertyId == MouseModePropertyId)
        // {
        //     Godot.Input.MouseMode = newValue.As<Godot.Input.MouseModeEnum>();
        // }
        // if (propertyId == CurrentCamera2DPropertyId)
        // {
        //     newValue.As<Camera2D>()?.MakeCurrent();
        // }
        // if (propertyId == CurrentCamera3DPropertyId)
        // {
        //     newValue.As<Camera3D>()?.MakeCurrent();
        //     prevValue.As<Camera3D>()?.ClearCurrent();
        // }

        if (NetworkSyncedAttributes != null && NetworkSyncedAttributes.Contains(attributeId) && AttributeIndex.IsMultiplayerAuthority())
        {
            this.GdfRpc(MethodName.SyncAttribute, attributeId, newValue);
        }
    }

    [GdfRpc(GdfConstants.DefaultRpcChannelPresetName)]
    private void SyncAttribute(StringName attributeId, Variant newValue)
    {
        CheckNetworkChanged();
        _networkSyncedFrame ??= AttributeIndex.CreateSetModifier("Network Synced", 999)
            .BindToNode(this);

        // Set via Modify.Set<> which guarantees set behavior
        if (!_networkSyncedFrame.TryModify(attributeId, new Mod.Set<Variant>(newValue)))
            // otherwise pass value as-is and hope the attribute treats it as set behavior
            _networkSyncedFrame.Modify(attributeId, newValue);
        
        _networkSyncedAuthority ??= AttributeIndex.GetMultiplayerAuthority();
    }

    public bool IsObserving(StringName attributeId)
    {
        return _observedAttributes.ContainsKey(attributeId);
    }

    public void StartObserving(StringName attributeId)
    {
        if (IsObserving(attributeId)) return;
        var attr = AttributeIndex?.GetAttribute(attributeId);
        if (attr == null)
        {
            GdfLogger.Error($"Failed to start observing attribute '{attributeId}': no such attribute in index.");
            return;
        }
        if (attr is not IAttributeComputable<Variant> computable)
        {
            GdfLogger.Error($"Failed to start observing attribute '{attributeId}': Attribute is not computable as Variant.");
            return;
        }
        if (attr is not IAttributeModifiable<Variant> modifiable)
        {
            GdfLogger.Error($"Failed to start observing attribute '{attributeId}': Attribute is not modifiable as Variant.");
            return;
        }
        _observedAttributes[attributeId] = attr;
    }

    public void StopObserving(StringName attributeId)
    {
        if (!IsObserving(attributeId)) return;
        _observedAttributes.Remove(attributeId);
        _prevObservedStates.Remove(attributeId);
    }
    
    [GdfRpc]
    void IResynchronizable.Resync(int peerId)
    {
        if (NetworkSyncedAttributes is not { Count: > 0 }) return;
        foreach (StringName attributeId in NetworkSyncedAttributes)
        {
            this.GdfRpc(MethodName.SyncAttribute, attributeId, AttributeIndex.GetValue<Variant>(attributeId));
        }
    }

    public override void _Ready()
    {
        base._Ready();
        
        if (NetworkSyncedAttributes is { Count: > 0 })
            foreach(StringName attributeId in NetworkSyncedAttributes) StartObserving(attributeId);
        if (AutoObservedAttributes is { Count: > 0 })
            foreach(StringName attributeId in AutoObservedAttributes) StartObserving(attributeId);
        
        this.RequestResync();
    }

    public override void _Process(double delta)
    {
        if (UpdateMode == UpdateModeEnum.Idle) Observe();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (UpdateMode == UpdateModeEnum.Physics) Observe();
    }

    private void FetchIndexIfNecessary()
    {
        if (AttributeIndex != null) return;
        AttributeIndex = this.GetComponent<AttributeIndex>();
    }

    public override void _Notification(int what)
    {
        base._Notification(what);
        switch ((long)what)
        {
            case NotificationEnterTree:
            case GdfConstants.NotificationDeepSceneInstantiated:
                FetchIndexIfNecessary();
                break;
        }
    }
    
    public enum UpdateModeEnum
    {
        Idle,
        Physics,
        Manual
    }

    private struct WatchedAttributeState
    {
        public int EditCount;
        public Variant Value;
    }
}