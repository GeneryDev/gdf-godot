using System;
using System.Collections.Generic;
using GDF.Logical.Signals;
using GDF.Util;
using Godot;

namespace GDF.Data;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/node_factory.png")]
public partial class NodeFactory : Node, IDataContext, IDataQueryOptions
{
    [Signal]
    public delegate void UpdatedEventHandler();

    [Signal]
    public delegate void NodeCreatedEventHandler(Node node);
    [Signal]
    public delegate void NodeRemovedEventHandler(Node node);

    [Export]
    public DataQueryType QueryType
    {
        get => _queryType;
        set
        {
            if (_queryType == value) return;
            _queryType = value;
            OnContextUpdated();
        }
    }

    [Export(PropertyHint.MultilineText)]
    public string CollectionQuery
    {
        get => _collectionQuery;
        set
        {
            _collectionQuery = value;
            if(QueryType is DataQueryType.Collection)
                OnContextUpdated();
        }
    }

    [Export(PropertyHint.MultilineText)]
    public string SubContextQuery
    {
        get => _subContextQuery;
        set
        {
            _subContextQuery = value;
            if(QueryType is DataQueryType.SubContext)
                OnContextUpdated();
        }
    }

    [Export]
    public Node DataContext
    {
        get => _contextNode;
        set
        {
            if (Engine.IsEditorHint())
            {
                _contextNode = value;
                return;
            }
            (_contextNode as IDataContext)?.DisconnectUpdateSignal(new Callable(this, MethodName.OnContextUpdated));
            _contextNode = value;
            (_contextNode as IDataContext)?.ConnectUpdateSignal(new Callable(this, MethodName.OnContextUpdated));
            OnContextUpdated();
        }
    }

    [Export]
    public Godot.Collections.Dictionary<StringName, NodePath> DataContextsBySlot
    {
        get => _dataContextsBySlot;
        set
        {
            if (Engine.IsEditorHint())
            {
                _dataContextsBySlot = value;
                return;
            }
            if (_dataContextsBySlot != null)
                foreach(var nodePath in _dataContextsBySlot.Values)
                    (GetNodeOrNull(nodePath) as IDataContext)?.DisconnectUpdateSignal(new Callable(this, MethodName.OnContextUpdated));
            _dataContextsBySlot = value;
            if (_dataContextsBySlot != null)
                foreach(var nodePath in _dataContextsBySlot.Values)
                    (GetNodeOrNull(nodePath) as IDataContext)?.ConnectUpdateSignal(new Callable(this, MethodName.OnContextUpdated));
        }
    }

    [Export] public NodeTemplate DefaultTemplate;

    [Export] public Godot.Collections.Array<NodeFactoryConditionalTemplate> ConditionalTemplates;

    [ExportGroup("Optimization")]
    [Export] public UpdateModeEnum UpdateMode = UpdateModeEnum.Automatic;
    [Export] public bool ThrottleUpdate = true;
    [Export] public bool UpdateOutsideTree = true;
    [Export(PropertyHint.Enum,"Never,If Not Updated Prior,Always")] public int UpdateOnTreeEntered = 1;
    
    [ExportGroup("Query Options")]
    [Export] public bool SupportsNullOperands { get; set; } = false;
    [Export] public int FontSize { get; set; } = 16;
    [Export] public bool BbcodeEnabled { get; set; } = false;

    [ExportSubgroup("Deferred Instantiation")]
    [Export(PropertyHint.GroupEnable)] public bool UseDeferredInstantiation;
    [Export] public int MaximumImmediateInstantiationCount = 8;
    [Export(PropertyHint.Range,"0,1,0.01,or_greater,suffix:s")] public float InstantiationInterval = 0.0f;
    [Export] public int MaximumPeriodicInstantiationCount = 8;

    [ExportGroup("Tree")]
    [Export(PropertyHint.Enum,@"Disabled,Enabled at default template,Enabled grouped by template")] public ItemSortModeEnum ItemSortMode = ItemSortModeEnum.EnabledAtDefaultTemplate;
    [Export] public bool EnableNodeReuse = true;
    [Export] public NodeReuseConditionsEnum NodeReuseConditions = NodeReuseConditionsEnum.ContextsEqual | NodeReuseConditionsEnum.TemplatesEqual;
    [Export] public string NodeNameFormat;
    [Export] public string MultiplayerAuthorityQuery;
    [Export] public SignalStation ConnectSignalStation;
    [Export] public bool EmitNodeUpdateSignals = false;
    [Export] public bool InjectFactoryItemContext = false;

    private string _collectionQuery;
    private string _subContextQuery;
    private Node _contextNode;
    private Godot.Collections.Dictionary<StringName, NodePath> _dataContextsBySlot;
    private bool _updateQueued = false;
    private bool _everUpdated = false;

    private readonly List<FactoryItemEntry> _createdItems = new();

    private List<IDataContext> _collectedContexts = new();
    private List<FactoryItemEntry> _newOrReusedItemList = new();
    private List<(IDataContext Context, int Index)> _remainingContextsToAdd = new();
    private Queue<(IDataContext Context, int Index)> _instantiationQueue = new();
    private int _instantiationQueueModCount = 0;
    private Accumulator _instantiationQueueAccumulator;
    private DataQueryType _queryType = DataQueryType.Collection;
    
    private ParsedDataQuery _queryCache;
    private ParsedDataQuery _nameQueryCache;
    private ParsedDataQuery _authorityQueryCache;
    private Dictionary<NodePath, NodeTemplate> _cachedTemplatesByPath = new();

    public override void _Ready()
    {
        RequestReady();
        if (UpdateMode is UpdateModeEnum.Automatic)
        {
            switch (UpdateOnTreeEntered)
            {
                case 0: break;
                case 1 when !_everUpdated:
                    Update();
                    break;
                case 2:
                    Update();
                    break;
            }
        }
    }

    private void OnContextUpdated()
    {
        if (Engine.IsEditorHint()) return;
        if (UpdateMode is UpdateModeEnum.Automatic)
            Update();
    }


    public void Update()
    {
        if (Engine.IsEditorHint()) return;
        if (!UpdateOutsideTree && !IsInsideTree()) return;
        if (ThrottleUpdate)
        {
            if (!_updateQueued)
            {
                CallDeferred(MethodName.ExecuteUpdate);
                _updateQueued = true;
            }
        }
        else
        {
            ExecuteUpdate();
        }
    }

    public void ExecuteUpdate()
    {
        if (Engine.IsEditorHint()) return;
        _updateQueued = false;
        if (!UpdateOutsideTree && !IsInsideTree()) return;
        _everUpdated = true;
        _instantiationQueue.Clear();
        var newCollection = _collectedContexts;
        newCollection.Clear();
        switch (QueryType)
        {
            case DataQueryType.Collection:
            {
                this.EvaluateCollection(CollectionQuery, newCollection, ref _queryCache, this);
                break;
            }
            case DataQueryType.SubContext:
            {
                var subContext = !string.IsNullOrEmpty(SubContextQuery) ? this.EvaluateSubContext(SubContextQuery, ref _queryCache, this) : _contextNode as IDataContext;
                if (subContext != null)
                {
                    newCollection.Add(subContext);
                }

                break;
            }
        }

        _newOrReusedItemList.Clear();
        // _newOrReusedItemList here means new item list
        _remainingContextsToAdd.Clear();
        for (int index = 0; index < newCollection.Count; index++)
        {
            _remainingContextsToAdd.Add((newCollection[index], index));
        }
        // _remainingContextsToAdd here means contexts left to assign to items

        // Find old nodes to reuse.
        for (var i = 0; i < _createdItems.Count; i++)
        {
            if (!EnableNodeReuse) break;
            var item = _createdItems[i];

            if (!item.IsValid) continue;

            for (var j = 0; j < newCollection.Count; j++)
            {
                var newCtx = newCollection[j];
                int indexInRemainingList = -1;
                for (var k = 0; k < _remainingContextsToAdd.Count; k++)
                {
                    if (_remainingContextsToAdd[k].Index == j)
                    {
                        indexInRemainingList = k;
                    }
                }
                
                if (indexInRemainingList == -1) continue;
                if ((NodeReuseConditions & NodeReuseConditionsEnum.ContextsEqual) != 0 &&
                    !newCtx.EqualsContext(item.ItemContext)) continue;
                var templateToUse = GetTemplateForContext(newCtx);
                if ((NodeReuseConditions & NodeReuseConditionsEnum.TemplatesEqual) != 0 &&
                    ConditionalTemplates is { Count: > 0 } && item.UsedTemplate != templateToUse) continue;
                // can reuse
                _remainingContextsToAdd.RemoveAt(indexInRemainingList);
                var newItem = item with
                {
                    IndexInCollection = j,
                    ItemContext = newCtx
                };
                InjectContextToItem(ref newItem);
                _newOrReusedItemList.Add(newItem);
                _createdItems.RemoveAt(i);
                i--;
                break;
            }
        }

        var anyRemoved = false;

        // Remove old nodes not reused.
        foreach (var item in _createdItems)
        {
            if(item.IsValid)
                EmitSignalNodeRemoved(item.CreatedNode);
            item.Remove();
            anyRemoved = true;
        }
        
        // transfer (reused) items from _tempItems to _createdItems
        _createdItems.Clear();
        while (_createdItems.Count < newCollection.Count) _createdItems.Add(default);
        foreach (var item in _newOrReusedItemList) _createdItems[item.IndexInCollection] = item;
        _newOrReusedItemList.Clear();
        
        // assemble instantiation queue
        _instantiationQueue.Clear();
        _instantiationQueueModCount++;
        _instantiationQueueAccumulator.Reset();

        foreach (var remainingEntry in _remainingContextsToAdd)
        {
            _instantiationQueue.Enqueue(remainingEntry);
        }

        newCollection.Clear();
        if (_instantiationQueue.Count != 0)
        {
            if (UseDeferredInstantiation)
            {
                ProcessInstantiationQueue(MaximumImmediateInstantiationCount);
            }
            else
            {
                ProcessInstantiationQueue(_instantiationQueue.Count);
            }
        }
        else if (anyRemoved)
        {
            EmitSignalUpdated();
        }
    }

    private void ProcessInstantiationQueue(int maxInstantiationCount)
    {
        if (_instantiationQueue.Count == 0) return;
        var modCount = _instantiationQueueModCount;
        var remainingInstantiations = maxInstantiationCount;
        while (modCount == _instantiationQueueModCount &&
               remainingInstantiations > 0 &&
               _instantiationQueue.TryDequeue(out var entry))
        {
            var newCtx = entry.Context;
            var index = entry.Index;
            var templateToUse = GetTemplateForContext(newCtx);
            if (templateToUse == null) continue; //skip

            var newTask = templateToUse.New();
            if (newTask.Instance == null) continue;
            if (!string.IsNullOrEmpty(NodeNameFormat))
            {
                if (NodeNameFormat == "random")
                {
                    newTask.SetName("" + newTask.Instance.GetInstanceId());
                }
                else
                {
                    newTask.SetName(newCtx.Format(NodeNameFormat, ref _nameQueryCache));
                }
            }
            if (!string.IsNullOrEmpty(MultiplayerAuthorityQuery))
                newTask.Instance.SetMultiplayerAuthority(newCtx.Evaluate(MultiplayerAuthorityQuery, ref _authorityQueryCache).AsInt32());

            var item = new FactoryItemEntry()
            {
                CreatedNode = newTask.Instance,
                ItemContext = newCtx,
                UsedTemplate = templateToUse,
                IndexInCollection = index
            };
            InjectContextToItem(ref item);
            _createdItems[index] = item;
            newTask.Insert();
            remainingInstantiations--;

            if (ConnectSignalStation != null)
                SignalUtils.ConnectSignalStation(ConnectSignalStation, newTask.Instance);

            if (EmitNodeUpdateSignals)
            {
                EmitSignalNodeCreated(item.CreatedNode);
            }
        }
        
        if (ItemSortMode != ItemSortModeEnum.Disabled) SortItems();

        bool finished = _instantiationQueue.Count == 0;
        if (finished)
        {
            EmitSignalUpdated();
        }
    }

    private void InjectContextToItem(ref FactoryItemEntry item)
    {
        var node = item.CreatedNode;
        var context = item.ItemContext;
        node.InjectContext(context);
        if (_dataContextsBySlot != null)
        {
            foreach (var (slotId, contextNode) in _dataContextsBySlot)
            {
                node.InjectContext(slotId, this.GetNodeOrNull(contextNode) as IDataContext);
            }
        }

        if (InjectFactoryItemContext)
        {
            node.InjectContext("factory_item", new FactoryItemEntryContext(item, this));
        }
    }

    private void SortItems()
    {
        for (int i = _createdItems.Count - 1; i >= 0; i--)
        {
            var item = _createdItems[i];
            var itemNode = item.CreatedNode;
            var parent = itemNode?.GetParent();
            if (parent == null) continue;
            var toIndex = 0;
            var indexReferenceNode = ItemSortMode switch
            {
                ItemSortModeEnum.EnabledAtDefaultTemplate => DefaultTemplate,
                ItemSortModeEnum.EnabledGroupedByTemplate => item.UsedTemplate,
                _ => null
            };
            if (parent == indexReferenceNode?.GetParent())
            {
                toIndex = indexReferenceNode.GetIndex() + 1;
                if (itemNode.GetIndex() < toIndex - 1)
                {
                    // This will be moved down in the scene tree rather than up, so compensate
                    toIndex--;
                }
            }
            
            parent.MoveChild(itemNode, toIndex);
        }
    }

    private NodeTemplate GetTemplateForContext(IDataContext item)
    {
        if (ConditionalTemplates is {Count: > 0})
            foreach (var entry in ConditionalTemplates)
            {
                if (entry.EvaluateConditionQuery(item))
                    return GetTemplateAtPath(entry.TemplatePath);
            }
        return DefaultTemplate;
    }

    private NodeTemplate GetTemplateAtPath(NodePath path)
    {
        if (_cachedTemplatesByPath.TryGetValue(path, out var template)) return template;
        return _cachedTemplatesByPath[path] = this.GetNode<NodeTemplate>(path);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (UseDeferredInstantiation && _instantiationQueue.Count > 0)
        {
            _instantiationQueueAccumulator.Add((float)delta);
            bool instantiateThisFrame = false;
            if (InstantiationInterval > 0)
            {
                while (_instantiationQueueAccumulator.Consume(InstantiationInterval))
                {
                    instantiateThisFrame = true;
                }
            }
            else
            {
                instantiateThisFrame = true;
            }

            if (instantiateThisFrame)
            {
                ProcessInstantiationQueue(MaximumPeriodicInstantiationCount);
            }
        }
    }

    StringName IDataContext.UpdatedSignalName => SignalName.Updated;
    IDataContext IDataContext.ParentContext => DataContext as IDataContext;

    public bool GetContextVariable(string key, string input, ref Variant output, IDataQueryOptions options)
    {
        switch (key)
        {
            case "created_item_count":
            {
                return this.OutputIntVariable(_createdItems.Count, ref output, input);
            }
        }

        return false;
    }

    public override void _Notification(int what)
    {
        if (Engine.IsEditorHint()) return;
        if (what == NotificationPredelete)
        {
            (_contextNode as IDataContext)?.DisconnectUpdateSignal(new Callable(this, MethodName.OnContextUpdated));
            if (_dataContextsBySlot != null)
                foreach(var nodePath in _dataContextsBySlot.Values)
                    (GetNodeOrNull(nodePath) as IDataContext)?.DisconnectUpdateSignal(new Callable(this, MethodName.OnContextUpdated));
        }
    }

    public List<Node> GetCreatedNodes()
    {
        var list = new List<Node>();
        foreach (var item in _createdItems) list.Add(item.CreatedNode);

        return list;
    }

    public bool GetCreatedNodeInfo(Node node, out IDataContext itemContext, out int indexInCollection)
    {
        itemContext = null;
        indexInCollection = -1;
        foreach (var item in _createdItems)
        {
            if (item.CreatedNode == node)
            {
                itemContext = item.ItemContext;
                indexInCollection = item.IndexInCollection;
                return true;
            }
        }

        return false;
    }

#if TOOLS
    public override void _ValidateProperty(Godot.Collections.Dictionary property)
    {
        var propName = property["name"].AsStringName();
        var usage = property["usage"].As<PropertyUsageFlags>();

        if (propName == PropertyName.QueryType)
            usage |= PropertyUsageFlags.UpdateAllIfModified;

        if (propName == PropertyName.CollectionQuery &&
            QueryType is not DataQueryType.Collection)
            usage &= ~(PropertyUsageFlags.Editor | PropertyUsageFlags.Storage);
        if (propName == PropertyName.SubContextQuery &&
            QueryType is not DataQueryType.SubContext)
            usage &= ~(PropertyUsageFlags.Editor | PropertyUsageFlags.Storage);

        property["usage"] = Variant.From(usage);
    }
#endif

    private struct FactoryItemEntry
    {
        public Node CreatedNode;
        public IDataContext ItemContext;
        public NodeTemplate UsedTemplate;
        public int IndexInCollection;

        public bool IsValid => IsInstanceValid(CreatedNode);

        public void Remove()
        {
            if (CreatedNode != null && IsInstanceValid(CreatedNode) && CreatedNode.GetParent() is { } parent)
            {
                parent.RemoveChild(CreatedNode);
                CreatedNode.QueueFree();
            }
        }
    }

    public enum UpdateModeEnum
    {
        Manual,
        Automatic
    }
    
    private struct FactoryItemEntryContext : IDataContext, ICacheableDataContext<FactoryItemEntryContext>
    {
        public readonly int IndexInCollection;
        public readonly NodeFactory Factory;
        
        public FactoryItemEntryContext(FactoryItemEntry item, NodeFactory factory)
        {
            this.IndexInCollection = item.IndexInCollection;
            Factory = factory;
        }

        public bool GetContextVariable(string key, string input, ref Variant output, IDataQueryOptions options)
        {
            switch (key)
            {
                case "index_in_collection":
                {
                    output = IndexInCollection;
                    return true;
                }
            }

            return false;
        }

        public bool GetSubContext(string key, string input, ref IDataContext output, IDataQueryOptions options)
        {
            switch (key)
            {
                case "factory":
                case "factory_context":
                {
                    output = Factory;
                    return true;
                }
            }

            return false;
        }

        public bool EqualsContext(FactoryItemEntryContext otherCtx)
        {
            return otherCtx.IndexInCollection == this.IndexInCollection && otherCtx.Factory == this.Factory;
        }

        public bool CanCache() => true;
    }

    public enum ItemSortModeEnum
    {
        Disabled,
        EnabledAtDefaultTemplate,
        EnabledGroupedByTemplate
    }

    [Flags]
    public enum NodeReuseConditionsEnum
    {
        TemplatesEqual = 1 << 0,
        ContextsEqual = 1 << 1
    }
}