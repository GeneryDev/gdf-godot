using System.Collections.Generic;
using Godot;

namespace GDF.Data;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/node_factory.png")]
public partial class NodeCollectionInjector : Node, IDataContext, IDataContextCollectionInjectable
{
    [Signal]
    public delegate void UpdatedEventHandler();
    
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

    [Export] public UpdateModeEnum UpdateMode = UpdateModeEnum.Automatic;
    
    [Export]
    public Node[] Items;

    [Export] public bool UpdateVisibility = true;
    
    private DataQueryType _queryType = DataQueryType.Collection;
    private string _collectionQuery;
    private string _subContextQuery;
    private Node _contextNode;
    private Godot.Collections.Dictionary<StringName, NodePath> _dataContextsBySlot;
    private ParsedDataQuery _queryCache;
    
    private List<IDataContext> _collectedContexts = new();
    
    public override void _Ready()
    {
        RequestReady();
        OnContextUpdated();
    }

    private void OnContextUpdated()
    {
        if (Engine.IsEditorHint()) return;
        if (UpdateMode is UpdateModeEnum.Automatic)
            Update();
    }

    public void Update()
    {
        var newCollection = _collectedContexts;
        newCollection.Clear();
        switch (QueryType)
        {
            case DataQueryType.Collection:
            {
                this.EvaluateCollection(CollectionQuery, newCollection, ref _queryCache);
                break;
            }
            case DataQueryType.SubContext:
            {
                var subContext = !string.IsNullOrEmpty(SubContextQuery) ? this.EvaluateSubContext(SubContextQuery, ref _queryCache) : _contextNode as IDataContext;
                if (subContext != null)
                {
                    newCollection.Add(subContext);
                }

                break;
            }
        }
        InjectCollection(newCollection);
        newCollection.Clear();
    }
    
    public void InjectCollection(List<IDataContext> collection)
    {
        if (Items == null) return;
        for (int i = 0; i < Items.Length; i++)
        {
            var item = Items[i];
            if(item == null) continue;
            var itemContext = i < collection.Count ? collection[i] : null;
            UpdateItem(item, itemContext);
        }
        EmitSignalUpdated();
    }

    private void UpdateItem(Node item, IDataContext itemContext)
    {
        bool valid = itemContext != null;
        if (item.IsClass(nameof(Node2D)) || item.IsClass(nameof(Node3D)) || item.IsClass(nameof(Control)))
        {
            item.Set(Node3D.PropertyName.Visible, valid);
        }
        item.InjectContext(itemContext);
        if (_dataContextsBySlot != null)
        {
            foreach (var (slotId, contextNode) in _dataContextsBySlot)
            {
                item.InjectContext(slotId, this.GetNodeOrNull(contextNode) as IDataContext);
            }
        }
    }

    StringName IDataContext.UpdatedSignalName => SignalName.Updated;
    IDataContext IDataContext.ParentContext => DataContext as IDataContext;

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

    public enum UpdateModeEnum
    {
        Manual,
        Automatic
    }
}