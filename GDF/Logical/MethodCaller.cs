using System.Linq;
using GDF.Editor;
using GDF.Logical.Signals;
using GDF.Logical.Values;
using GDF.Util;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

namespace GDF.Logical;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/method_caller.png")]
public partial class MethodCaller : TriggerableLogicNode, IInboundArgumentSource
{
    [Export]
    public Node Target
    {
        get => _target;
        set
        {
            if (_target == value) return;
            _target = value;
            DiscardCache();
            NotifyPropertyListChanged();
        }
    }

    [Export]
    public StringName Method
    {
        get => _method;
        set
        {
            if (_method == value) return;
            _method = value;
            DiscardCache();
            RefreshPropertyListOnDelay(1);
        }
    }

    [Export(PropertyHint.Enum,"Any:-1,0:0,1:1,2:2,3:3,4:4,5:5,6:6,7:7,8:8")]
    public int FilterByArgumentCount
    {
        get => _filterByArgumentCount;
        set
        {
            if (_filterByArgumentCount == value) return;
            _filterByArgumentCount = value;
            DiscardCache();
            NotifyPropertyListChanged();
        }
    }

    [Export]
    public Array Args = new();

    [Export]
    public Array<ArgumentInputType> ArgInputTypes
    {
        get => _argInputTypes;
        set
        {
            _argInputTypes = value;
            DiscardCache();
            NotifyPropertyListChanged();
        }
    }

    [Export]
    public Array<bool> ArgReplicate = new();

    [Export] public Dictionary TargetMethodInfo = new();

    private StringName _method = "";
    private Node _target;
    private Array<ArgumentInputType> _argInputTypes = new();
    private int _filterByArgumentCount = -1;
    
    private Array _triggeredWithArgs;
    private Array<Dictionary> _argPropertyList = null;
    private Array<Dictionary> _argInputTypePropertyList = null;
    private Array<Dictionary> _argReplicatePropertyList = null;
    
    public void Trigger()
    {
        TriggerWithArgs(null);
    }

    public void Trigger(Variant p0)
    {
        TriggerWithArgs(new Array {p0});
    }

    public void Trigger(Variant p0, Variant p1)
    {
        TriggerWithArgs(new Array {p0, p1});
    }

    public void Trigger(Variant p0, Variant p1, Variant p2)
    {
        TriggerWithArgs(new Array {p0, p1, p2});
    }

    public void Trigger(Variant p0, Variant p1, Variant p2, Variant p3)
    {
        TriggerWithArgs(new Array {p0, p1, p2, p3});
    }

    public void Trigger(Variant p0, Variant p1, Variant p2, Variant p3, Variant p4)
    {
        TriggerWithArgs(new Array {p0, p1, p2, p3, p4});
    }

    public void Trigger(Variant p0, Variant p1, Variant p2, Variant p3, Variant p4, Variant p5)
    {
        TriggerWithArgs(new Array {p0, p1, p2, p3, p4, p5});
    }

    public void Trigger(Variant p0, Variant p1, Variant p2, Variant p3, Variant p4, Variant p5, Variant p6)
    {
        TriggerWithArgs(new Array {p0, p1, p2, p3, p4, p5, p6});
    }

    public void Trigger(Variant p0, Variant p1, Variant p2, Variant p3, Variant p4, Variant p5, Variant p6, Variant p7)
    {
        TriggerWithArgs(new Array {p0, p1, p2, p3, p4, p5, p6, p7});
    }

    public void Trigger(Variant p0, Variant p1, Variant p2, Variant p3, Variant p4, Variant p5, Variant p6, Variant p7,
        Variant p8)
    {
        TriggerWithArgs(new Array {p0, p1, p2, p3, p4, p5, p6, p7, p8});
    }

    public void TriggerWithArgs(Array triggerArgs)
    {
        _triggeredWithArgs = triggerArgs;
        
        if (!RunInEditor && Engine.IsEditorHint()) return;
        if (!ExecuteOutsideTree && !IsInsideTree()) return;
        if (!AuthorityMode.CanExecute(this)) return;

        var args = new Array();
        if (!PopulateArgumentArray(args)) return;

        if (ReplicateToPeers)
        {
            if (AnyArgsReplicated())
            {
                Rpc(MethodName.TriggerWithArgsRpc, args);
                ExecuteWithArgs(args);
            }
            else
            {
                Rpc(TriggerableLogicNode.MethodName.TriggerRpc);
            }
        }
        else
        {
            ExecuteWithArgs(args);
        }
    }

    private bool AnyArgsReplicated()
    {
        foreach (bool replicated in ArgReplicate)
        {
            if (replicated) return true;
        }

        return false;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer,
        CallLocal = false,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,
        TransferChannel = GdfConstants.DefaultRpcTransferChannel)]
    private void TriggerWithArgsRpc(Array args)
    {
        if (!PopulateArgumentArray(args, skipReplicated: true)) return;
        ExecuteWithArgs(args);
    }

    protected override Empty Execute()
    {
        var args = new Array();
        if (!PopulateArgumentArray(args)) return default;
        return ExecuteWithArgs(args);
    }
    
    private Empty ExecuteWithArgs(Array args)
    {
        if (Target == null) return default;
        Target.Callv(Method, args);
        return base.Execute();
    }

    private bool PopulateArgumentArray(in Array args, bool skipReplicated = false)
    {
        if (!EnsureArgumentsReady())
        {
            GD.PrintErr($"Failed to invoke {nameof(MethodCaller)} with method {Method}: too soon!\nat {this.GetSceneAndPathString()}");
            return false;
        }
        args.Resize(_argPropertyList.Count);

        for (var i = 0; i < _argPropertyList.Count; i++)
        {
            if (skipReplicated && ArgReplicate[i]) continue;
            var arg = Args[i];
            if (ArgInputTypes[i] == ArgumentInputType.ValueSource)
                if (arg.VariantType == Variant.Type.Object && arg.AsGodotObject() is IValueSource argValueSource)
                    arg = argValueSource.GetValue(this);
            
            args[i] = arg;
        }

        return true;
    }
    
    private void DiscardCache()
    {
        _argPropertyList = null;
        _argInputTypePropertyList = null;
        _argReplicatePropertyList = null;
    }

    private bool EnsureArgumentsReady()
    {
        if (!EnsurePropertyListReady()) return false;

        if (Args.Count != _argPropertyList.Count)
        {
            while (Args.Count > _argPropertyList.Count)
            {
                Args.RemoveAt(Args.Count-1);
            }
            while (Args.Count < _argPropertyList.Count)
            {
                int index = Args.Count;
                Args.Add(_argPropertyList[index]["default"]);
            }
        }
        if (ArgInputTypes.Count != _argPropertyList.Count)
        {
            while (ArgInputTypes.Count > _argPropertyList.Count)
            {
                ArgInputTypes.RemoveAt(ArgInputTypes.Count-1);
            }
            while (ArgInputTypes.Count < _argPropertyList.Count)
            {
                ArgInputTypes.Add(ArgumentInputType.Constant);
            }
        }
        if (ArgReplicate.Count != _argPropertyList.Count)
        {
            while (ArgReplicate.Count > _argPropertyList.Count)
            {
                ArgReplicate.RemoveAt(ArgReplicate.Count-1);
            }
            while (ArgReplicate.Count < _argPropertyList.Count)
            {
                ArgReplicate.Add(false);
            }
        }

        return true;
    }

    private bool EnsurePropertyListReady()
    {
        if (Target == null) return false;
        if (_argPropertyList != null && _argInputTypePropertyList != null && _argReplicatePropertyList != null) return true;
        
        UpdatePropertyList();
        return true;
    }

    private Dictionary GetTargetMethodInfo()
    {
        if (!Engine.IsEditorHint() && TargetMethodInfo is { Count: > 0 }) return TargetMethodInfo;
        
        if (Target == null) return null;
        foreach (var methodInfo in Target.GetMethodList())
        {
            var methodName = methodInfo["name"].AsStringName();
            if (methodName != Method) continue;
            if (FilterByArgumentCount != -1)
            {
                if (methodInfo["args"].AsGodotArray().Count != FilterByArgumentCount) continue;
            }
            return methodInfo;
        }

        return null;
    }

    private void DumpMethodArgumentProperties(Dictionary info)
    {
        if (info == null || info.Count == 0) return;
        var args = info["args"].AsGodotArray();
        var rawDefaults = info["default_args"];
        var defaults = rawDefaults.VariantType != Variant.Type.Nil ? rawDefaults.AsGodotArray() : null;
        int argCount = args.Count;

        for (int i = 0; i < argCount; i++)
        {
            var argInfo = args[i].AsGodotDictionary();
            string argName = argInfo["name"].AsString();
            
            var inputType = ArgumentInputType.Constant;
            if (ArgInputTypes != null && i < ArgInputTypes.Count)
            {
                inputType = ArgInputTypes[i];
            }
            
            var type = argInfo["type"].As<Variant.Type>();
            var className = argInfo["class_name"].AsStringName();
            var hint = PropertyHint.None;
            var hintString = "";
            var usage = PropertyUsageFlags.Editor;

            switch (inputType)
            {
                case ArgumentInputType.Constant:
                {
                    if (type == Variant.Type.Object && !string.IsNullOrEmpty(className))
                    {
                        if (ClassDB.IsParentClass(className, nameof(Resource)))
                        {
                            hint = PropertyHint.ResourceType;
                            hintString = className;
                        }
                    }

                    break;
                }
                case ArgumentInputType.ValueSource:
                    type = Variant.Type.Object;
                    className = nameof(ValueSource);
                    break;
            }
            
            _argPropertyList.Add(new Dictionary()
            {
                {"name", $"args/{i}_{argName}"},
                {"class_name", className},
                {"type", Variant.From(type)},
                {"hint", Variant.From(hint)},
                {"hint_string", hintString},
                {"usage", Variant.From(usage)},
                {"default", inputType == ArgumentInputType.Constant ? defaults?.ElementAtOrDefault(i - (args.Count - defaults.Count)) ?? default : default}
            });
            
            _argInputTypePropertyList.Add(new Dictionary()
            {
                {"name", $"configure_args/{i}_{argName}/input_type"},
                {"class_name", ""},
                {"type", Variant.From(Variant.Type.Int)},
                {"hint", Variant.From(PropertyHint.Enum)},
                {"hint_string", "Constant,Value Source"},
                {"usage", Variant.From(PropertyUsageFlags.Editor)},
                {"default", 0}
            });
            
            _argReplicatePropertyList.Add(new Dictionary()
            {
                {"name", $"configure_args/{i}_{argName}/replicate"},
                {"class_name", ""},
                {"type", Variant.From(Variant.Type.Bool)},
                {"hint", Variant.From(PropertyHint.None)},
                {"hint_string", ""},
                {"usage", Variant.From(PropertyUsageFlags.Editor)},
                {"default", false}
            });
            
        }
    }

    public override bool _Set(StringName property, Variant value)
    {
        if (EnsureArgumentsReady())
        {
            var argIndex = 0;
            foreach (var propertyInfo in _argPropertyList)
            {
                if (propertyInfo["name"].AsStringName() == property)
                {
                    Args[argIndex] = value;
                }

                argIndex++;
            }
            argIndex = 0;
            foreach (var propertyInfo in _argInputTypePropertyList)
            {
                if (propertyInfo["name"].AsStringName() == property)
                {
                    ArgInputTypes[argIndex] = value.As<ArgumentInputType>();
                    NotifyPropertyListChanged();
                }

                argIndex++;
            }
            argIndex = 0;
            foreach (var propertyInfo in _argReplicatePropertyList)
            {
                if (propertyInfo["name"].AsStringName() == property)
                {
                    ArgReplicate[argIndex] = value.AsBool();
                }

                argIndex++;
            }
        }
        return base._Set(property, value);
    }

    public override Variant _Get(StringName property)
    {
        if (EnsureArgumentsReady())
        {
            
            var argIndex = 0;
            foreach (var propertyInfo in _argPropertyList)
            {
                if (propertyInfo["name"].AsStringName() == property)
                {
                    return Args[argIndex];
                }

                argIndex++;
            }
            argIndex = 0;
            foreach (var propertyInfo in _argInputTypePropertyList)
            {
                if (propertyInfo["name"].AsStringName() == property)
                {
                    return Variant.From(ArgInputTypes[argIndex]);
                }

                argIndex++;
            }
            argIndex = 0;
            foreach (var propertyInfo in _argReplicatePropertyList)
            {
                if (propertyInfo["name"].AsStringName() == property)
                {
                    return ArgReplicate[argIndex];
                }

                argIndex++;
            }
        }
        return base._Get(property);
    }

    public override bool _PropertyCanRevert(StringName property)
    {
        EnsurePropertyListReady();
        foreach (var propertyInfo in _argPropertyList)
        {
            if (propertyInfo["name"].AsStringName() == property)
            {
                return true;
            }
        }
        foreach (var propertyInfo in _argInputTypePropertyList)
        {
            if (propertyInfo["name"].AsStringName() == property)
            {
                return true;
            }
        }
        foreach (var propertyInfo in _argReplicatePropertyList)
        {
            if (propertyInfo["name"].AsStringName() == property)
            {
                return true;
            }
        }
        return base._PropertyCanRevert(property);
    }

    public override Variant _PropertyGetRevert(StringName property)
    {
        EnsurePropertyListReady();
        foreach (var propertyInfo in _argPropertyList)
        {
            if (propertyInfo["name"].AsStringName() == property)
            {
                return propertyInfo["default"];
            }
        }
        foreach (var propertyInfo in _argInputTypePropertyList)
        {
            if (propertyInfo["name"].AsStringName() == property)
            {
                return Variant.From(ArgumentInputType.Constant);
            }
        }
        foreach (var propertyInfo in _argReplicatePropertyList)
        {
            if (propertyInfo["name"].AsStringName() == property)
            {
                return false;
            }
        }
        return base._PropertyGetRevert(property);
    }

    public override Array<Dictionary> _GetPropertyList()
    {
        UpdatePropertyList();
        var arr = new Array<Dictionary>();
        arr.AddRange(_argPropertyList);
        arr.AddRange(_argInputTypePropertyList);
        arr.AddRange(_argReplicatePropertyList);
        return arr;
    }

    private void UpdatePropertyList()
    {
        _argPropertyList ??= new();
        _argPropertyList.Clear();
        _argInputTypePropertyList ??= new();
        _argInputTypePropertyList.Clear();
        _argReplicatePropertyList ??= new();
        _argReplicatePropertyList.Clear();
        var targetMethodInfo = GetTargetMethodInfo();
        TargetMethodInfo = targetMethodInfo;
        if (targetMethodInfo != null)
        {
            // GD.Print(EditorUtils.GetMethodSignatureText(targetMethodInfo));
            DumpMethodArgumentProperties(targetMethodInfo);
        }
    }

    private Tween _notifyUpdatedTween;
    private void RefreshPropertyListOnDelay(float delay)
    {
        _notifyUpdatedTween?.Kill();
        _notifyUpdatedTween = CreateTween().SetIgnoreTimeScale(true);
        _notifyUpdatedTween.TweenInterval(delay);
        _notifyUpdatedTween.TweenCallback(new Callable(this, GodotObject.MethodName.NotifyPropertyListChanged));
    }

    public override void _ValidateProperty(Dictionary property)
    {
        var propertyName = property["name"].AsStringName();
        var usage = property["usage"].As<PropertyUsageFlags>();
        if (propertyName == PropertyName.Args || propertyName == PropertyName.ArgInputTypes || propertyName == PropertyName.ArgReplicate || propertyName == PropertyName.TargetMethodInfo)
        {
            usage &= ~(PropertyUsageFlags.Editor);
        }

        property["usage"] = Variant.From(usage);
        base._ValidateProperty(property);
    }

#if TOOLS
    private NodePath _popupSelectedNodePath;
    private StringName _popupSelectedMethodName;

    [InspectorCustomControl(AnchorProperty = nameof(Target), AnchorMode = InspectorPropertyAnchorMode.Before)]
    public Control SelectMethod()
    {
        var button = new Button();
        button.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        button.Icon = EditorInterface.Singleton.GetEditorTheme().GetIcon("Slot", "EditorIcons");
        if(Target != null && !string.IsNullOrEmpty(Method))
            button.Text = EditorUtils.GetMethodSignatureText(GetTargetMethodInfo());
        else
            button.Text = "Select Method...";
        button.Pressed += ShowNodePicker;
        
        return button;
    }

    private void ShowNodePicker()
    {
        EditorInterface.Singleton.PopupNodeSelector(new Callable(this, MethodName.OnNodeSelected));
    }

    private void OnNodeSelected(NodePath nodePath)
    {
        if (nodePath is not { IsEmpty: false }) return;
        
        var source = EditorInterface.Singleton.GetInspector().GetEditedObject() as Node;
        if (source == null)
        {
            GD.PrintErr("Currently inspected object is not a node. Cannot continue method selection.");
            return;
        }
        
        var node = (source.Owner ?? source).GetNode(nodePath);
        GD.Print($"Selected node: {node}");

        if (node == null)
        {
            GD.PrintErr("Selected node path does not point to a node? Cannot continue method selection.");
            return;
        }
        _popupSelectedNodePath = source.GetPathTo(node);

        ShowMethodPicker(node);
    }

    private void ShowMethodPicker(Node node)
    {
        EditorUtils.ShowSignalMethodSelector(node, 0, null, OnMethodSelected);
    }

    private void OnMethodSelected(Dictionary method)
    {
        var methodName = method["name"].AsStringName();
        _popupSelectedMethodName = methodName;
        GD.Print($"Selected method: {methodName}");
        CommitPopupChange();
    }

    private void CommitPopupChange()
    {
        var undoRedo = EditorInterface.Singleton.GetEditorUndoRedo();
        undoRedo.CreateAction("Set MethodCaller Method");
        undoRedo.AddDoProperty(this, PropertyName.Target, this.GetNode(_popupSelectedNodePath));
        undoRedo.AddDoProperty(this, PropertyName.Method, _popupSelectedMethodName);
        undoRedo.AddDoProperty(this, PropertyName.Args, new Array());
        undoRedo.AddDoMethod(this, GodotObject.MethodName.NotifyPropertyListChanged);
        undoRedo.AddUndoProperty(this, PropertyName.Target, Target);
        undoRedo.AddUndoProperty(this, PropertyName.Method, Method);
        undoRedo.AddUndoProperty(this, PropertyName.Args, Args);
        undoRedo.AddUndoMethod(this, GodotObject.MethodName.NotifyPropertyListChanged);
        undoRedo.CommitAction();
    }
#endif

    public enum ArgumentInputType
    {
        Constant,
        ValueSource
    }

    public Variant GetArgument(int index)
    {
        if (_triggeredWithArgs != null && index >= 0 && index < _triggeredWithArgs.Count)
        {
            return _triggeredWithArgs[index];
        }

        return default;
    }
}