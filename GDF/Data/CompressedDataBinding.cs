using GDF.Util;
using Godot;
using Godot.Collections;

namespace GDF.Data;

[Tool]
[GlobalClass]
public partial class CompressedDataBinding : Resource
{
    [Export] public DataQuery Query;
    
    [ExportSubgroup("Value Mapping")]
    [Export(PropertyHint.GroupEnable)] public bool ValueMappingEnabled = false;
    [Export] public Dictionary ValueMappings = new();
    [Export] public Variant ValueMappingDefault;
    
    [ExportGroup("Binding Target")] 
    [Export] public NodePath TargetNode;
    [Export]
    public string TargetPropertyName
    {
        get => _targetPropertyName;
        set
        {
            if (_targetPropertyName == value) return;
            _targetPropertyName = value;
            _targetPropertyPath = !string.IsNullOrEmpty(value) ? new NodePath(value) : null;
        }
    }
    [Export] public bool SendUpdatedSignalWithValue = false;
    [Export] public bool SendBooleanEvaluationSignals = false;
    [Export] public StringName InjectingSlotId = "";
    
    [ExportGroup("Optimization")]
    [Export] public bool ThrottleUpdate = true;
    [Export] public bool UpdateOutsideTree = true;
    [Export(PropertyHint.Enum,"Never,If Not Updated Prior,Always")] public int UpdateOnTreeEntered = 1;
    [Export] public bool FireOnlyOnNonNullValues = false;
    [Export] public bool FireOnlyOnValueChange = false;
    
    [ExportGroup("Compressor Data")]
    [Export] public NodePath UncompressedNodePath;
    [Export] public int UncompressedNodeIndex;
    [Export] public Dictionary<StringName, Array<Dictionary>> CompressedConnections = new();
    
    private string _targetPropertyName;
    private NodePath _targetPropertyPath;
    
    public NodePath TargetPropertyPath => _targetPropertyPath;

    public void Compress(DataBinding binding, DataBindingCompressor compressor)
    {
        Query ??= new();
        Query.DataContext = compressor.GetPathTo(binding.DataContext);
        Query.Query = binding.Query;
        Query.QueryType = binding.QueryType;
        Query.DefaultValue = binding.DefaultValue;
        Query.BbcodeEnabled = binding.BbcodeEnabled;
        Query.FontSize = binding.FontSize;
        Query.SupportsNullOperands = binding.SupportsNullOperands;
        
        if (binding.TargetNode != null)
            this.TargetNode = compressor.GetPathTo(binding.TargetNode);
        else
            this.TargetNode = null;

        this.TargetPropertyName = binding.TargetPropertyName;
        this.SendUpdatedSignalWithValue = binding.SendUpdatedSignalWithValue;
        this.SendBooleanEvaluationSignals = binding.SendBooleanEvaluationSignals;
        this.InjectingSlotId = binding.InjectingSlotId;
        this.ValueMappingEnabled = binding.ValueMappingEnabled;
        this.ValueMappings = binding.ValueMappings;
        this.ValueMappingDefault = binding.ValueMappingDefault;

        this.ThrottleUpdate = binding.ThrottleUpdate;
        this.UpdateOutsideTree = binding.UpdateOutsideTree;
        this.UpdateOnTreeEntered = binding.UpdateOnTreeEntered;
        this.FireOnlyOnNonNullValues = binding.FireOnlyOnNonNullValues;
        this.FireOnlyOnValueChange = binding.FireOnlyOnValueChange;
        
        CompressedConnections.Clear();
        CompressSignals(binding, compressor, DataBinding.SignalName.Updated);
        CompressSignals(binding, compressor, DataBinding.SignalName.UpdatedWithValue);
        CompressSignals(binding, compressor, DataBinding.SignalName.EvaluatedTrue);
        CompressSignals(binding, compressor, DataBinding.SignalName.EvaluatedFalse);
    }

    private void CompressSignals(DataBinding binding, DataBindingCompressor compressor, StringName signalName)
    {
        foreach (var connection in binding.GetSignalConnectionList(signalName))
        {
            var connectionCallable = connection["callable"].AsCallable();
            if (connectionCallable.Target is not Node targetNode) continue;
            var flags = connection["flags"].As<ConnectFlags>();
            if ((flags & ConnectFlags.Persist) == 0) continue;
            var compressedConnection = new Dictionary()
            {
                { "target", compressor.GetPathTo(targetNode) },
                { "method", connectionCallable.Method },
                { "flags", Variant.From(flags) }
            };

            if (!CompressedConnections.TryGetValue(signalName, out var connectionsForSignal))
            {
                connectionsForSignal = CompressedConnections[signalName] = new Array<Dictionary>();
            }
            
            connectionsForSignal.Add(compressedConnection);
        }
    }

    public void Decompress(DataBinding binding, DataBindingCompressor compressor)
    {
        if (Query != null)
        {
            binding.DataContext = compressor.GetNodeOrNull(Query.DataContext);
            binding.Query = Query.Query;
            binding.QueryType = Query.QueryType;
            binding.DefaultValue = Query.DefaultValue;
            binding.BbcodeEnabled = Query.BbcodeEnabled;
            binding.FontSize = Query.FontSize;
            binding.SupportsNullOperands = Query.SupportsNullOperands;
        }
        
        if (this.TargetNode != null)
            binding.TargetNode = compressor.GetNodeOrNull(this.TargetNode);
        else
            binding.TargetNode = null;

        binding.TargetPropertyName = this.TargetPropertyName;
        binding.SendUpdatedSignalWithValue = this.SendUpdatedSignalWithValue;
        binding.SendBooleanEvaluationSignals = this.SendBooleanEvaluationSignals;
        binding.InjectingSlotId = this.InjectingSlotId;
        binding.ValueMappingEnabled = this.ValueMappingEnabled;
        binding.ValueMappings = this.ValueMappings;
        binding.ValueMappingDefault = this.ValueMappingDefault;

        binding.ThrottleUpdate = this.ThrottleUpdate;
        binding.UpdateOutsideTree = this.UpdateOutsideTree;
        binding.UpdateOnTreeEntered = this.UpdateOnTreeEntered;
        binding.FireOnlyOnNonNullValues = this.FireOnlyOnNonNullValues;
        binding.FireOnlyOnValueChange = this.FireOnlyOnValueChange;
        
        DecompressSignals(binding, compressor, DataBinding.SignalName.Updated);
        DecompressSignals(binding, compressor, DataBinding.SignalName.UpdatedWithValue);
        DecompressSignals(binding, compressor, DataBinding.SignalName.EvaluatedTrue);
        DecompressSignals(binding, compressor, DataBinding.SignalName.EvaluatedFalse);
    }

    private void DecompressSignals(DataBinding binding, DataBindingCompressor compressor, StringName signalName)
    {
        if (!CompressedConnections.TryGetValue(signalName, out var connectionsForSignal)) return;
        foreach (var compressedConnection in connectionsForSignal)
        {
            var target = compressor.GetNodeOrNull(compressedConnection["target"].AsNodePath());
            var method = compressedConnection["method"].AsStringName();
            var flags = compressedConnection["flags"].As<ConnectFlags>();

            var callable = new Callable(target, method);
            binding.TryConnect(signalName, callable, flags);
        }
    }

    public Variant RemapValue(Variant input)
    {
        if (!ValueMappingEnabled) return input;
        if (ValueMappings?.ContainsKey(input) ?? false) return ValueMappings[input];
        return ValueMappingDefault;
    }

#if TOOLS
    public override void _ValidateProperty(Dictionary property)
    {
        var propName = property["name"].AsStringName();
        var usage = property["usage"].As<PropertyUsageFlags>();

        if (propName == PropertyName.ValueMappings &&
            !ValueMappingEnabled)
            usage &= ~PropertyUsageFlags.Storage;

        if ((propName == PropertyName.InjectingSlotId) &&
            Query?.QueryType is not DataQueryType.SubContext)
            usage &= ~(PropertyUsageFlags.Editor | PropertyUsageFlags.Storage);

        property["usage"] = Variant.From(usage);
    }
#endif
}