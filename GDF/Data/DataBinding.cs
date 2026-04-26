using System.Collections.Generic;
using GDF.Editor;
using GDF.Logical.Values;
using GDF.Util;
using Godot;
using Godot.Collections;

namespace GDF.Data;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/data_binding.png")]
public partial class DataBinding : Node, IDataContext, IDataQueryOptions
{
    [Signal]
    public delegate void UpdatedEventHandler();

    [Signal]
    public delegate void UpdatedWithValueEventHandler(Variant value);

    [Signal]
    public delegate void EvaluatedTrueEventHandler();
    [Signal]
    public delegate void EvaluatedFalseEventHandler();

    [Signal]
    public delegate void ParentContextUpdatedEventHandler();


    [Export]
    public Node DataContext
    {
        get => _contextNode;
        set
        {
            (_contextNode as IDataContext)?.DisconnectUpdateSignal(new Callable(this, MethodName.OnParentContextUpdated));
            _signalsConnected = false;
            _contextNode = value;
            if (RunInEditor || !Engine.IsEditorHint())
            {
                (_contextNode as IDataContext)?.ConnectUpdateSignal(new Callable(this, MethodName.OnParentContextUpdated));
                _signalsConnected = true;
            }
            OnParentContextUpdated();
        }
    }
    
    [Export(PropertyHint.MultilineText)]
    public string Query
    {
        get => _query;
        set
        {
            if (_query == value) return;
            _query = value;
            OnPropertiesUpdated();
        }
    }

    [Export] public DataQueryType QueryType = DataQueryType.Expression;

    [Export]
    public ValueSource DefaultValue
    {
        get => _defaultValue;
        set
        {
            if (_defaultValue == value) return;
            _defaultValue = value;
            OnPropertiesUpdated();
        }
    }

    [ExportGroup("Value Mapping")]
    [Export(PropertyHint.GroupEnable)] public bool ValueMappingEnabled = false;
    [Export] public Dictionary ValueMappings = new();
    [Export] public Variant ValueMappingDefault;

    [ExportGroup("Binding Target")]
    [Export]
    public Node TargetNode
    {
        get => _targetNode;
        set
        {
            if (_targetNode == value) return;
            _targetNode = value;
            OnPropertiesUpdated();
            if(Engine.IsEditorHint()) NotifyPropertyListChanged();
        }
    }

    [Export]
    public string TargetPropertyName
    {
        get => _targetPropertyName;
        set
        {
            if (_targetPropertyName == value) return;
            _targetPropertyName = value;
            _targetPropertyPath = !string.IsNullOrEmpty(value) ? new NodePath(value) : null;
            OnPropertiesUpdated();
        }
    }

    [Export] public bool SendUpdatedSignalWithValue = false;
    [Export] public bool SendBooleanEvaluationSignals = false;
    [Export] public StringName InjectingSlotId = "";

    [ExportGroup("Optimization")]
    [Export]
    public UpdateModeEnum UpdateMode
    {
        get => _updateMode;
        set
        {
            if (_updateMode == value) return;
            _updateMode = value;
            OnPropertiesUpdated();
        }
    }

    [Export] public bool ThrottleUpdate = true;
    [Export] public bool UpdateOutsideTree = true;
    [Export(PropertyHint.Enum,"Never,If Not Updated Prior,Always")] public int UpdateOnTreeEntered = 1;
    [Export] public bool FireOnlyOnNonNullValues = false;
    [Export] public bool FireOnlyOnValueChange = false;
    [Export] public bool Compress = true;
    
    [ExportGroup("Query Options")]
    [Export] public bool SupportsNullOperands { get; set; } = false;
    [Export] public int FontSize { get; set; } = 16;
    [Export] public bool BbcodeEnabled { get; set; } = false;

    [ExportGroup("Editor")]
    [ExportToolButton("Suggest Node Name")] private Callable ButtonSuggestNodeName => new Callable(this, MethodName.SuggestNodeName);

    [Export]
    public bool RunInEditor
    {
        get => _runInEditor;
        set
        {
            if (_runInEditor == value) return;
            _runInEditor = value;
            OnPropertiesUpdated();
        }
    }

    private string _query;
    private Node _contextNode;
    private bool _updateQueued = false;
    private Variant _prevValue;
    private bool _everUpdated = false;
    private bool _signalsConnected = false;
    
    private ParsedDataQuery _queryCache;
    private ValueSource _defaultValue;
    private Node _targetNode;
    private string _targetPropertyName;
    private NodePath _targetPropertyPath;
    private UpdateModeEnum _updateMode = UpdateModeEnum.Automatic;
    private bool _runInEditor = false;

    private bool _sceneComplete = false;

    private void Update()
    {
        if(!RunInEditor && Engine.IsEditorHint())
            return;
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

    private void ExecuteUpdate()
    {
        _updateQueued = false;
        if (!UpdateOutsideTree && !IsInsideTree()) return;
        _everUpdated = true;
        Variant value;
        switch (QueryType)
        {
            case DataQueryType.Expression:
                value = this.Evaluate(Query, ref _queryCache, options: this);
                break;
            case DataQueryType.String:
                value = this.Format(Query, ref _queryCache, options: this);
                break;
            case DataQueryType.SubContext:
                value = default;
                var subContextValue = !string.IsNullOrEmpty(Query) ? this.EvaluateSubContext(Query, ref _queryCache, options: this) : _contextNode as IDataContext;
                if (subContextValue == null && _defaultValue is DataQuery { QueryType: DataQueryType.SubContext } defaultContext)
                {
                    subContextValue = defaultContext.GetValueAsContext(this);
                }
                TargetNode.InjectContext(InjectingSlotId, subContextValue);
                break;
            case DataQueryType.Collection:
            {
                value = default;
                var collection = new List<IDataContext>();
                this.EvaluateCollection(Query, collection, ref _queryCache, options: this);
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
        
        EmitSignalUpdated();
    }

    private void FireForValue(Variant value)
    {
        if (value.VariantType == Variant.Type.Nil)
            value = GetDefaultValue();

        if (ValueMappingEnabled)
            value = RemapValue(value);

        if (FireOnlyOnNonNullValues && value.VariantType == Variant.Type.Nil)
            return;

        if (FireOnlyOnValueChange)
        {
            if (value.VariantEquals(_prevValue))
            {
                return;
            }
            _prevValue = value;
        }

        if (TargetNode != null && !_targetPropertyPath.IsNullOrEmpty())
            TargetNode.SetIndexed(_targetPropertyPath, value);

        if (SendUpdatedSignalWithValue && QueryType != DataQueryType.SubContext)
            EmitSignalUpdatedWithValue(value);

        if (SendBooleanEvaluationSignals && QueryType != DataQueryType.SubContext)
            if(value.AsBool())
                EmitSignalEvaluatedTrue();
            else
                EmitSignalEvaluatedFalse();
    }

    private Variant RemapValue(Variant input)
    {
        if (!ValueMappingEnabled) return input;
        if (ValueMappings?.ContainsKey(input) ?? false) return ValueMappings[input];
        return ValueMappingDefault;
    }

    public override void _Ready()
    {
        RequestReady();
        MarkSceneComplete(update: false);
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

    private Variant GetDefaultValue()
    {
        return DefaultValue?.GetValue(this) ?? default;
    }

    private void OnParentContextUpdated()
    {
        EmitSignalParentContextUpdated();
        if (_sceneComplete && UpdateMode is UpdateModeEnum.Automatic)
            Update();
    }

    private void OnPropertiesUpdated()
    {
        if (_sceneComplete && UpdateMode is UpdateModeEnum.Automatic)
            Update();
    }

    public void SetQuery(string query)
    {
        Query = query;
    }

    public string GetQuery()
    {
        return Query;
    }
    
    IDataContext IDataContext.ParentContext => DataContext as IDataContext;
    StringName IDataContext.UpdatedSignalName => SignalName.ParentContextUpdated;

    int? IDataQueryOptions.FontSize => FontSize;
    bool? IDataQueryOptions.BbcodeEnabled => BbcodeEnabled;

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged && QueryType == DataQueryType.String)
            Update();
        if (what == NotificationPredelete && _signalsConnected)
            (_contextNode as IDataContext)?.DisconnectUpdateSignal(new Callable(this, MethodName.OnParentContextUpdated));
        if (what == GdfConstants.NotificationDeepSceneInstantiated)
            MarkSceneComplete(update: true);
    }

    private void MarkSceneComplete(bool update)
    {
        if (_sceneComplete) return;
        _sceneComplete = true;
        if (update && UpdateMode is UpdateModeEnum.Automatic)
            Update();
    }

    private void SuggestNodeName()
    {
#if TOOLS
        string suggestedName = GetSuggestedNodeName();
        if (suggestedName == null) return;
        
        var undoRedo = EditorInterface.Singleton.GetEditorUndoRedo();
        undoRedo.CreateAction("Set Node Name");
        undoRedo.AddDoProperty(this, Node.PropertyName.Name, suggestedName);
        undoRedo.AddUndoProperty(this, Node.PropertyName.Name, Name);
        undoRedo.CommitAction();
#endif
    }

    private string GetSuggestedNodeName()
    {
        return GetSuggestedNodeName(QueryType, TargetPropertyName);
    }

    private static string GetSuggestedNodeName(DataQueryType queryType, string targetPropertyName)
    {
        string suggestedName = null;
        switch (queryType)
        {
            case DataQueryType.Expression:
            case DataQueryType.String:
                if (targetPropertyName == "visible")
                    suggestedName = "Visibility Binding";
                else if (targetPropertyName != null)
                {
                    if (targetPropertyName.Contains(':'))
                    {
                        suggestedName = targetPropertyName[(targetPropertyName.LastIndexOf(':')+1)..] + " Binding";
                    }
                    else
                    {
                        suggestedName = targetPropertyName.Capitalize() + " Binding";
                    }
                }
                break;
            case DataQueryType.SubContext:
                suggestedName = "Context Binding";
                break;
            case DataQueryType.Collection:
                suggestedName = "Collection Binding";
                break;
        }

        return suggestedName;
    }

    private void SetTargetNodeParent()
    {
#if TOOLS
        var undoRedo = EditorInterface.Singleton.GetEditorUndoRedo();
        undoRedo.CreateAction("Set Parent as Target Node");
        undoRedo.AddDoProperty(this, PropertyName.TargetNode, GetParent());
        undoRedo.AddUndoProperty(this, PropertyName.TargetNode, TargetNode);
        undoRedo.CommitAction();
#endif
    }

    private void OpenPicker()
    {
#if TOOLS
        if (QueryType is DataQueryType.Expression or DataQueryType.String)
        {
            EditorUtils.ShowNodeAndPropertyPicker(this, TargetNode ?? this, (nodePath, propertyPath) =>
            {
                var undoRedo = EditorInterface.Singleton.GetEditorUndoRedo();
                undoRedo.CreateAction("Set Target Node and Property");
                undoRedo.AddDoProperty(this, PropertyName.TargetNode, this.GetNode(nodePath));
                undoRedo.AddDoProperty(this, PropertyName.TargetPropertyName, propertyPath);
                undoRedo.AddDoProperty(this, Node.PropertyName.Name, GetSuggestedNodeName(QueryType, propertyPath));
                undoRedo.AddUndoProperty(this, PropertyName.TargetNode, TargetNode);
                undoRedo.AddUndoProperty(this, PropertyName.TargetPropertyName, TargetPropertyName);
                undoRedo.AddUndoProperty(this, Node.PropertyName.Name, Name);
                undoRedo.CommitAction();
            });
        }
        else
        {
            EditorUtils.ShowNodePicker(this, TargetNode ?? this, nodePath =>
            {
                var undoRedo = EditorInterface.Singleton.GetEditorUndoRedo();
                undoRedo.CreateAction("Set Target Node");
                undoRedo.AddDoProperty(this, PropertyName.TargetNode, this.GetNode(nodePath));
                undoRedo.AddUndoProperty(this, PropertyName.TargetNode, TargetNode);
                undoRedo.CommitAction();
            });
        }
#endif
    }
    
#if TOOLS
    [InspectorCustomControl(AnchorProperty = nameof(TargetNode), AnchorMode = InspectorPropertyAnchorMode.Before)]
    public Control TargetPropertyCustomControl()
    {
        var container = new HBoxContainer()
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        
        if (TargetNode != null && GetParent() != TargetNode)
        {
            var parentButton = new Button();
            container.AddChild(parentButton);
            parentButton.Icon = EditorInterface.Singleton.GetEditorTheme().GetIcon("Warning", "EditorIcons");
            parentButton.TooltipText = "Target node is not the binding's parent. Click to change to parent.";
            parentButton.Connect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.SetTargetNodeParent));
        }
        
        var mainButton = new Button();
        container.AddChild(mainButton);
        mainButton.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        mainButton.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        mainButton.Icon = EditorUtils.GetObjectIcon(TargetNode) ?? EditorInterface.Singleton.GetEditorTheme().GetIcon("Node", "EditorIcons");
        if (TargetNode != null)
        {
            if (QueryType is DataQueryType.Expression or DataQueryType.String)
            {
                if (!string.IsNullOrEmpty(TargetPropertyName))
                {
                    mainButton.Text = $"{TargetNode.Name} : {TargetPropertyName}";
                }
                else
                {
                    mainButton.Text = $"{TargetNode.Name}";
                }
            }
            else
            {
                mainButton.Text = $"{TargetNode.Name} -> {(string.IsNullOrEmpty(InjectingSlotId) ? "(default)" : InjectingSlotId)}";
            }
        }
        else if (QueryType is DataQueryType.Expression or DataQueryType.String)
        {
            mainButton.Text = "Select Node and Property...";
        }
        else
        {
            mainButton.Text = "Select Node...";
        }
        mainButton.Connect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.OpenPicker));
        
        return container;
    }
#endif

    public bool IsEligibleForCompression()
    {
        if (!Compress) return false;
        if (this.GetChildCount() > 0) return false;
        if (DataContext == null) return false;
        if (UpdateMode != UpdateModeEnum.Automatic) return false;
        if (RunInEditor) return false;
        if (DefaultValue != null && DefaultValue is not ConstantValue) return false;
        
        return true;
    }

#if TOOLS
    public override void _ValidateProperty(Dictionary property)
    {
        var propName = property["name"].AsStringName();
        var usage = property["usage"].As<PropertyUsageFlags>();

        if (propName == PropertyName.QueryType || propName == PropertyName.ValueMappingEnabled || propName == PropertyName.UpdateMode)
            usage |= PropertyUsageFlags.UpdateAllIfModified;

        if ((propName == PropertyName.TargetPropertyName || propName == PropertyName.SendUpdatedSignalWithValue || propName == PropertyName.SendBooleanEvaluationSignals) &&
            QueryType is not (DataQueryType.Expression or DataQueryType.String))
            usage &= ~(PropertyUsageFlags.Editor | PropertyUsageFlags.Storage);

        if ((propName == PropertyName.ValueMappingEnabled || propName == PropertyName.ValueMappings || propName == PropertyName.ValueMappingDefault) &&
            QueryType is not (DataQueryType.Expression or DataQueryType.String))
            usage &= ~(PropertyUsageFlags.Editor | PropertyUsageFlags.Storage);

        if ((propName == PropertyName.UpdateOnTreeEntered) &&
            UpdateMode is not UpdateModeEnum.Automatic)
            usage &= ~(PropertyUsageFlags.Editor | PropertyUsageFlags.Storage);

        if (propName == PropertyName.ValueMappings &&
            !ValueMappingEnabled)
            usage &= ~PropertyUsageFlags.Storage;

        if ((propName == PropertyName.InjectingSlotId) &&
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