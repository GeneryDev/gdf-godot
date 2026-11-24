using GDF.Attributes.Impl;
using GDF.Composition;
using GDF.DocHints;
using GDF.Networking;
using GDF.Util;
using Godot;
using Godot.Collections;

namespace GDF.Attributes;

[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/attribute_watcher.svg")]
public partial class AttributeWatcher : Node, IResynchronizable
{
    [Signal]
    public delegate void UpdatedEventHandler();
    [Signal]
    public delegate void AttributeUpdatedEventHandler(StringName id, Variant prevValue, Variant newValue);

    [Export] public AttributeIndex AttributeIndex;

    [Export] public UpdateModeEnum UpdateMode = UpdateModeEnum.Idle;
    
    [UsedOnlyAtInitialization([CommonAccessTimes.Ready])]
    [Export] public Array<StringName> ObservedAttributes = new();

    [ExportGroup("Network")]
    [UsedOnlyAtInitialization([CommonAccessTimes.Ready])] 
    [Export(PropertyHint.GroupEnable)] public bool UseNetworkSynchronization = false;
    [UsedOnlyAtInitialization([CommonAccessTimes.Ready])] 
    [Export] public Array<StringName> NetworkSyncedAttributes = new();
    
    private BulkModifier _networkSyncedFrame;
    private int? _networkSyncedAuthority;
    private readonly System.Collections.Generic.List<ObservedAttribute> _observations = new();

    private void Clear()
    {
        _observations.Clear();
        _networkSyncedFrame?.Clear();
        _networkSyncedFrame = null;
    }

    private void ClearLastObservedStates()
    {
        for (var i = 0; i < _observations.Count; i++)
        {
            _observations[i] = _observations[i] with { PrevObserved = null };
        }
    }
    
    public void Observe()
    {
        CheckNetworkChanged();
        for (var i = 0; i < _observations.Count; i++)
        {
            var watch = _observations[i];
            var computable = (IAttributeComputable<Variant>)watch.Attribute;
            Variant previousValue = default;
            Variant currentValue;

            var fireUpdated = false;
            int currentEditCount = watch.Attribute.GetEditCount();
            if (watch.PrevObserved is {} prevState)
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

            watch.PrevObserved = new ObservedAttributeState()
            {
                Value = currentValue,
                EditCount = currentEditCount
            };

            _observations[i] = watch;

            if (fireUpdated)
            {
                FireAttributeUpdated(watch, previousValue, currentValue);
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
            ClearLastObservedStates();
            _networkSyncedFrame?.Clear();
        }
    }

    private void FireAttributeUpdated(ObservedAttribute watch, Variant prevValue, Variant newValue)
    {
        // GdfLogger.Error($"[{Name}] Attribute changed: {attributeId} from {prevValue} to {newValue}");
        EmitSignalUpdated();
        if (!watch.AttributeId.IsNullOrEmpty())
        {
            var attributeId = watch.AttributeId;
            
            EmitSignalAttributeUpdated(attributeId, prevValue, newValue);

            if (UseNetworkSynchronization &&
                NetworkSyncedAttributes != null &&
                NetworkSyncedAttributes.Contains(attributeId) &&
                AttributeIndex != null &&
                AttributeIndex.IsMultiplayerAuthority())
            {
                this.GdfRpc(MethodName.SyncAttribute, attributeId, newValue);
            }
        }
    }

    [GdfRpc(GdfConstants.DefaultRpcChannelPresetName)]
    private void SyncAttribute(StringName attributeId, Variant newValue)
    {
        CheckNetworkChanged();
        _networkSyncedFrame ??= new BulkModifier("Network Synced", 999)
            .BindToNode(this);

        var attr = AttributeIndex.GetAttribute(attributeId);
        
        // Set via Modify.Set<> which guarantees set behavior
        if (!_networkSyncedFrame.TryModify(attr, new Mod.Set<Variant>(newValue)))
            // otherwise pass value as-is and hope the attribute treats it as set behavior
            _networkSyncedFrame.Modify(attr, newValue);
        
        _networkSyncedAuthority ??= AttributeIndex.GetMultiplayerAuthority();
    }

    public bool IsObserving(StringName attributeId)
    {
        foreach (var watch in _observations)
        {
            if (watch.AttributeId == attributeId) return true;
        }

        return false;
    }

    public void StartObserving(StringName attributeId)
    {
        if (IsObserving(attributeId)) return;
        var attribute = AttributeIndex?.GetAttribute(attributeId);
        if (attribute == null)
        {
            GdfLogger.Error($"Failed to start observing attribute '{attributeId}': no such attribute in index.");
            return;
        }
        StartObserving(attributeId, attribute);
    }

    public void StartObserving(StringName attributeId, IAttribute attribute)
    {
        if (IsObserving(attributeId)) return;
        
        if (attribute is not IAttributeComputable<Variant> computable)
        {
            GdfLogger.Error($"Failed to start observing attribute '{attributeId}': Attribute is not computable as Variant.");
            return;
        }
        if (UseNetworkSynchronization && attribute is not IModifiable<Variant> modifiable)
        {
            GdfLogger.Error($"Failed to start observing attribute '{attributeId}': Attribute is not modifiable as Variant. This is required for network synchronization.");
            return;
        }

        _observations.Add(new ObservedAttribute()
        {
            AttributeId = attributeId,
            Attribute = attribute
        });
    }

    public void StopObserving(StringName attributeId)
    {
        for (var i = 0; i < _observations.Count; i++)
        {
            if (_observations[i].AttributeId != attributeId) continue;
            _observations.RemoveAt(i);
            i--;
        }
    }

    public void StopObserving(IAttribute attribute)
    {
        for (var i = 0; i < _observations.Count; i++)
        {
            if (_observations[i].Attribute != attribute) continue;
            _observations.RemoveAt(i);
            i--;
        }
    }
    
    [GdfRpc]
    void IResynchronizable.Resync(int peerId)
    {
        if (!UseNetworkSynchronization) return;
        if (NetworkSyncedAttributes is not { Count: > 0 }) return;
        foreach (StringName attributeId in NetworkSyncedAttributes)
        {
            this.GdfRpc(MethodName.SyncAttribute, attributeId, AttributeIndex.GetValue<Variant>(attributeId));
        }
    }

    public override void _Ready()
    {
        base._Ready();
        
        if (UseNetworkSynchronization && NetworkSyncedAttributes is { Count: > 0 })
            foreach(var attributeId in NetworkSyncedAttributes) StartObserving(attributeId);
        if (ObservedAttributes is { Count: > 0 })
            foreach(var attributeId in ObservedAttributes) StartObserving(attributeId);
        
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

    private struct ObservedAttributeState
    {
        public int EditCount;
        public Variant Value;
    }

    private struct ObservedAttribute
    {
        public StringName AttributeId;
        public IAttribute Attribute;
        public ObservedAttributeState? PrevObserved;
    }
}