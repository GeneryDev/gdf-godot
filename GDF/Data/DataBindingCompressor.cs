using System.Collections.Generic;
using GDF.Util;
using Godot;
using Godot.Collections;

namespace GDF.Data;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/data_binding_compressor.png")]
public partial class DataBindingCompressor : Node
{
    [Export] public Array<CompressedDataBinding> CompressedBindings = new();
    [Export] public bool Enabled = false;

    [ExportToolButton("Collect Bindings")]
    public Callable ButtonCollectBindings => new Callable(this, MethodName.CollectBindings);

    private DataBindingInstance[] _instances;

    public override void _Ready()
    {
        base._Ready();
        if (Enabled && Engine.IsEditorHint())
        {
            DecompressBindings();
        }
        else
        {
            RequestReady();
            InvokeInstancesReady();
        }
    }
    private void InvokeInstancesReady()
    {
        if (_instances == null) return;
        for (int i = 0; i < _instances.Length; i++)
        {
            var instance = _instances[i];
            instance.Ready(this);
            _instances[i] = instance;
        }
    }

    private void Initialize()
    {
        if (CompressedBindings == null) return;
        _instances = new DataBindingInstance[CompressedBindings.Count];
        for (int i = 0; i < _instances.Length; i++)
        {
            var instance = new DataBindingInstance(CompressedBindings[i], i);
            instance.Initialize(this);
            _instances[i] = instance;
        }
    }

    private void CollectBindings()
    {
        List<DataBinding> collected = new();
        List<CompressedDataBinding> compressedBindingsToReuse = new();
        compressedBindingsToReuse.AddRange(CompressedBindings);
        CompressedBindings.Clear();
        CollectBindings(collected, Owner);
        foreach (var binding in collected)
        {
            GD.Print(Owner.GetPathTo(binding));
            var path = GetPathTo(binding);
            CompressedDataBinding compressed = null;
            for (int i = 0; i < compressedBindingsToReuse.Count; i++)
            {
                if (compressedBindingsToReuse[i]?.UncompressedNodePath == path)
                {
                    compressed = compressedBindingsToReuse[i];
                    compressedBindingsToReuse.RemoveAt(i);
                    break;
                }
            }

            compressed ??= new CompressedDataBinding();
            compressed.UncompressedNodePath = path;
            compressed.UncompressedNodeIndex = binding.GetIndex();
            compressed.ResourceName = binding.Name;
            CompressedBindings.Add(compressed);
            compressed.Compress(binding, this);
        }
    }

    private void CollectBindings(List<DataBinding> output, Node parent)
    {
        foreach (var child in parent.GetChildren())
        {
            if (child.Owner != Owner) continue;
            if (child is DataBinding binding && binding.IsEligibleForCompression())
            {
                output.Add(binding);
            }
            
            CollectBindings(output, child);
        }
    }

    private void DecompressBindings()
    {
        foreach (var compressed in CompressedBindings)
        {
            var binding = GetNodeOrNull<DataBinding>(compressed.UncompressedNodePath);
            if (binding == null)
            {
                var parentPath = compressed.UncompressedNodePath.ToString();
                parentPath = parentPath[0..parentPath.LastIndexOf('/')];
                string nodeName =
                    compressed.UncompressedNodePath.GetName(compressed.UncompressedNodePath.GetNameCount() - 1);
                var parent = GetNodeOrNull(parentPath);
                if (parent != null)
                {
                    binding = new DataBinding()
                    {
                        Name = nodeName
                    };
                    compressed.Decompress(binding, this);
                    parent.AddChild(binding);
                    parent.MoveChild(binding, compressed.UncompressedNodeIndex);
                    binding.Owner = Owner;
                }
                else
                {
                    GD.PrintErr($"Failed to get parent of binding: {compressed.UncompressedNodePath}");
                }
            }
            else
            {
                compressed.Decompress(binding, this);
            }
        }
    }

    public override void _Notification(int what)
    {
        if (!Enabled) return;
        if (Engine.IsEditorHint())
        {
            if (what == NotificationEditorPreSave)
            {
                CollectBindings();
                foreach (var compressedBinding in CompressedBindings)
                {
                    var node = GetNode(compressedBinding.UncompressedNodePath);
                    node?.SetOwner(null);
                }
            }
            else if (what == NotificationEditorPostSave)
            {
                foreach (var compressedBinding in CompressedBindings)
                {
                    var node = GetNode(compressedBinding.UncompressedNodePath);
                    node?.SetOwner(Owner);
                }
            }
        }
        else
        {
            if (what == GdfConstants.NotificationDeepSceneInstantiated)
            {
                Initialize();
            }
            if (what == NotificationPredelete)
            {
                FinalizeBindings();
            }
        }
    }

    public void UpdateBinding(int index)
    {
        var instance = _instances[index];
        instance.Update(this);
        _instances[index] = instance;
    }

    public void ExecuteBinding(int index)
    {
        var instance = _instances[index];
        instance.Execute(this);
        _instances[index] = instance;
    }
    
    private void FinalizeBindings()
    {
        if (_instances == null) return;
        for (int i = 0; i < _instances.Length; i++)
        {
            var instance = _instances[i];
            instance.Predelete(this);
            _instances[i] = instance;
        }
    }

    private struct DataBindingInstance
    {
        public CompressedDataBinding Binding;
        public int Index;
        public bool IsEmpty => Binding == null;
        
        private Node _contextNode;
        private Node _targetNode;
        private bool _updateQueued = false;
        private Variant _prevValue;
        private bool _everUpdated = false;
        private bool _signalsConnected = false;
        private System.Collections.Generic.Dictionary<StringName, (Callable Callable, ConnectFlags Flags)[]> Connections;
        private Callable _callableConnectedToContext;

        public Node TargetNode => _targetNode;

        public DataBindingInstance()
        {
        }

        public DataBindingInstance(CompressedDataBinding binding, int index)
        {
            Binding = binding;
            Index = index;
        }

        public void Initialize(DataBindingCompressor compressor)
        {
            int index = Index;
            _contextNode = compressor.GetNodeOrNull(Binding.Query.DataContext);
            _callableConnectedToContext = Callable.From(() => compressor.UpdateBinding(index));
            (_contextNode as IDataContext)?.ConnectUpdateSignal(_callableConnectedToContext);
            _signalsConnected = true;
            
            if (!Binding.TargetNode.IsNullOrEmpty())
                _targetNode = compressor.GetNodeOrNull(Binding.TargetNode);

            if (Binding.CompressedConnections != null)
            {
                Connections = new();
                foreach (var (signalName, compressedConnections) in Binding.CompressedConnections)
                {
                    var connections = new (Callable Callable, ConnectFlags Flags)[compressedConnections.Count];
                    Connections[signalName] = connections;
                    for (var i = 0; i < compressedConnections.Count; i++)
                    {
                        var compressedConnection = compressedConnections[i];
                        
                        var target = compressor.GetNodeOrNull(compressedConnection["target"].AsNodePath());
                        var method = compressedConnection["method"].AsStringName();
                        var flags = compressedConnection["flags"].As<ConnectFlags>();

                        var callable = new Callable(target, method);
                        connections[i] = (callable, flags);
                    }
                }
            }
        }

        public void Ready(DataBindingCompressor compressor)
        {
            switch (Binding.UpdateOnTreeEntered)
            {
                case 0: break;
                case 1 when !_everUpdated:
                    Update(compressor);
                    break;
                case 2:
                    Update(compressor);
                    break;
            }
        }

        public void Update(DataBindingCompressor compressor)
        {
            if (!Binding.UpdateOutsideTree && !compressor.IsInsideTree()) return;
            if (Binding.ThrottleUpdate)
            {
                if (!_updateQueued)
                {
                    compressor.CallDeferred(MethodName.ExecuteBinding, Index);
                    _updateQueued = true;
                }
            }
            else
            {
                Execute(compressor);
            }
        }

        public void Execute(DataBindingCompressor compressor)
        {
            _updateQueued = false;
            if (!Binding.UpdateOutsideTree && !compressor.IsInsideTree()) return;
            _everUpdated = true;
            Variant value;
            var query = Binding.Query;
            switch (query.QueryType)
            {
                case DataQueryType.Expression:
                case DataQueryType.String:
                    value = query.GetValue(compressor, _contextNode as IDataContext);
                    break;
                case DataQueryType.SubContext:
                    value = default;
                    
                    var subContextValue = query.GetValueAsContext(compressor, _contextNode as IDataContext);
                    TargetNode.InjectContext(Binding.InjectingSlotId, subContextValue);
                    break;
                case DataQueryType.Collection:
                {
                    value = default;
                    var collection = new List<IDataContext>();
                    query.EvaluateCollection(compressor, collection);
                    if(TargetNode is IDataContextCollectionInjectable collectionInjectable) collectionInjectable.InjectCollection(collection);
                    break;
                }
                default:
                    value = default;
                    break;
            }
        
            if (DataContextPerformance.InstanceExists)
                DataContextPerformance.Instance.AccumulatedBindingUpdates++;

            FireForValue(value);
        
            EmitSignal(DataBinding.SignalName.Updated);
        }

        private void FireForValue(Variant value)
        {
            if (Binding.ValueMappingEnabled)
                value = Binding.RemapValue(value);

            if (Binding.FireOnlyOnNonNullValues && value.VariantType == Variant.Type.Nil)
                return;

            if (Binding.FireOnlyOnValueChange)
            {
                if (value.VariantEquals(_prevValue))
                {
                    return;
                }
                _prevValue = value;
            }

            if (TargetNode != null && !Binding.TargetPropertyPath.IsNullOrEmpty())
                TargetNode.SetIndexed(Binding.TargetPropertyPath, value);

            if (Binding.SendUpdatedSignalWithValue && Binding.Query.QueryType != DataQueryType.SubContext)
                EmitSignal(DataBinding.SignalName.UpdatedWithValue, value);

            if (Binding.SendBooleanEvaluationSignals && Binding.Query.QueryType != DataQueryType.SubContext)
                if(value.AsBool())
                    EmitSignal(DataBinding.SignalName.EvaluatedTrue);
                else
                    EmitSignal(DataBinding.SignalName.EvaluatedFalse);
        }

        private void EmitSignal(StringName signalName, params Variant[] args)
        {
            if (Connections == null) return;
            if (!Connections.TryGetValue(signalName, out var signalConnections)) return;
            for (int i = 0; i < signalConnections.Length; i++)
            {
                var callable = signalConnections[i].Callable;
                var flags = signalConnections[i].Flags;
                if (flags == 0) continue; // all valid callbacks must at least have the persistent flag when saved in the editor.
                
                if ((flags & ConnectFlags.OneShot) != 0)
                {
                    signalConnections[i] = default;
                }

                if ((flags & ConnectFlags.Deferred) != 0)
                {
                    callable.CallDeferred(args);
                }
                else
                {
                    callable.Call(args);
                }
            }
        }

        public void Predelete(DataBindingCompressor compressor)
        {
            if (_signalsConnected)
                (_contextNode as IDataContext)?.DisconnectUpdateSignal(_callableConnectedToContext);
        }
    }
}