using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace GDF.Data;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/data_context_injectable.png")]
public partial class InjectedDataContext : Node, IDataContext, IDataContextInjectable
{
    [Signal]
    public delegate void UpdatedEventHandler();
    
    [Export] public StringName InjectableSlotId = "";

    [Export] public Godot.Collections.Dictionary<string, Variant> DefaultVariableValues;
    [Export] public Godot.Collections.Dictionary<string, Array> DefaultCollections;

    [ExportGroup("Editor")] 
    [Export] public Godot.Collections.Dictionary<string, Variant> EditorPreviewVariables;

    public IDataContext ParentContext
    {
        get => _parentContext;
        set
        {
            _parentContext?.DisconnectUpdateSignal(new Callable(this, MethodName.Update));
            _parentContext = value;
            _parentContext?.ConnectUpdateSignal(new Callable(this, MethodName.Update));
            Update();
        }
    }

    private IDataContext _parentContext;

    public IDataContext ItemContext
    {
        get => _itemContextNode;
        set
        {
            _itemContextNode?.DisconnectUpdateSignal(new Callable(this, MethodName.Update));
            _itemContextNode = value;
            _itemContextNode?.ConnectUpdateSignal(new Callable(this, MethodName.Update));
            Update();
        }
    }

    private IDataContext _itemContextNode;
    private bool _suppressUpdate = false;

    protected void StartSuppressingUpdate() => _suppressUpdate = true;
    protected void StopSuppressingUpdate() => _suppressUpdate = false;

    public void Update()
    {
        if (!_suppressUpdate)
            EmitSignalUpdated();
    }

    public StringName GetInjectableSlotId()
    {
        return InjectableSlotId;
    }

    public void SetContexts(IDataContext itemContext)
    {
        _suppressUpdate = true;
        ItemContext = itemContext;
        ParentContext = itemContext?.ParentContext;
        _suppressUpdate = false;
        Update();
    }

    public override void _Ready()
    {
        RequestReady();
        Update();
    }

    bool IDataContext.UseStringsAsVariables => _itemContextNode?.UseStringsAsVariables ?? true;
    bool IDataContext.UseVariablesAsStrings => _itemContextNode?.UseVariablesAsStrings ?? true;

    bool IDataContext.GetContextVariable(string key, string input, ref Variant output,
        IDataQueryOptions options)
    {
        if (EditorPreviewVariables != null && Engine.IsEditorHint())
            if (EditorPreviewVariables.TryGetValue(key, out var previewValue))
            {
                output = previewValue;
                return true;
            }

        if (_itemContextNode?.GetContextVariable(key, input, ref output, options) ?? false) return true;
        if (_parentContext?.GetContextVariable(key, input, ref output, options) ?? false) return true;

        if (DefaultVariableValues != null)
            if (DefaultVariableValues.TryGetValue(key, out var defaultValue))
            {
                output = defaultValue;
                return true;
            }

        return false;
    }

    bool IDataContext.GetContextString(string key, string input, ref string replacement,
        IDataQueryOptions options)
    {
        return _itemContextNode?.GetContextString(key, input, ref replacement, options) ?? false;
    }

    bool IDataContext.GetSubContext(string key, string input, ref IDataContext output,
        IDataQueryOptions options)
    {
        return _itemContextNode?.GetSubContext(key, input, ref output, options) ?? false;
    }

    bool IDataContext.GetContextCollection(string key, string input, List<IDataContext> output,
        IDataQueryOptions options)
    {
        if (_itemContextNode?.GetContextCollection(key, input, output, options) ?? false) return true;

        if (DefaultCollections != null)
            if (DefaultCollections.TryGetValue(key, out var rawElements))
            {
                foreach (var rawElem in rawElements)
                {
                    var elem = rawElem;
                    if (elem.VariantType == Variant.Type.NodePath) elem = GetNodeOrNull(elem.AsNodePath());

                    if (elem.VariantType == Variant.Type.Object && elem.AsGodotObject() is IDataContext context)
                        output.Add(context);
                }

                return true;
            }

        return false;
    }

    StringName IDataContext.UpdatedSignalName => SignalName.Updated;

    IDataContext IDataContext.ParentContext => ParentContext;

    public override void _Notification(int what)
    {
        if (what == NotificationPredelete)
        {
            _parentContext?.DisconnectUpdateSignal(new Callable(this, MethodName.Update));
            _itemContextNode?.DisconnectUpdateSignal(new Callable(this, MethodName.Update));
        }
    }
}