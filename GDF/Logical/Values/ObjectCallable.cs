using System.Text;
using GDF.Editor;
using Godot;
using Godot.Collections;

namespace GDF.Logical.Values;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/callable.png")]
public partial class ObjectCallable : ValueSource
{
    [Export]
    public ValueSource Target;
    [Export]
    public StringName Method;

    [Export] public ValueSource[] Args;
    
    [Export] public bool ErrorOnMissingTarget = true;
    
    public Variant Call(Node source)
    {
        if (Target == null) return default;
        var obj = Target.GetValue(source).As<GodotObject>();
        if (obj == null)
        {
            MissingTargetPrintError(source);
            return default;
        }
        return obj.Call(Method, EvaluateArgs(source));
    }
    public T Call<[MustBeVariant]T>(Node source)
    {
        if (Target == null) return default;
        var obj = Target.GetValue(source).As<GodotObject>();
        if (obj == null)
        {
            MissingTargetPrintError(source);
            return default;
        }
        return obj.Call(Method, EvaluateArgs(source)).As<T>();
    }
    
    public Variant CallWithArgs(Node source, params Variant[] args)
    {
        if (Target == null) return default;
        var obj = Target.GetValue(source).As<GodotObject>();
        if (obj == null)
        {
            MissingTargetPrintError(source);
            return default;
        }
        return obj?.Call(Method, args) ?? default;
    }
    public T CallWithArgs<[MustBeVariant]T>(Node source, params Variant[] args)
    {
        if (Target == null) return default;
        var obj = Target.GetValue(source).As<GodotObject>();
        if (obj == null)
        {
            MissingTargetPrintError(source);
            return default;
        }
        return obj.Call(Method, args).As<T>() ?? default;
    }

    public Variant[] EvaluateArgs(Node source)
    {
        if (Args == null) return null;
        var args = new Variant[Args.Length];
        for (int i = 0; i < Args.Length; i++)
        {
            args[i] = Args[i]?.GetValue(source) ?? default;
        }

        return args;
    }

    public override Variant GetValue(Node source)
    {
        return Call(source);
    }

    private void MissingTargetPrintError(Node source)
    {
        if (ErrorOnMissingTarget)
            GD.PrintErr($"Failed to execute {nameof(ObjectCallable)}, target returned null!\nIn: {(source?.IsInsideTree() ?? false ? source.GetPath() : source?.Name)}");
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append(Target);
        sb.Append("  :: ");
        sb.Append(Method);
        sb.Append('(');
        if (Args is { Length: > 0 })
        {
            for (var i = 0; i < Args.Length; i++)
            {
                sb.Append(Args[i]);
                if(i < Args.Length-1)
                    sb.Append(", ");
            }
        }
        sb.Append(')');
        return sb.ToString();
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
        if(Target != null && Method is {IsEmpty: false})
            button.Text = this.ToString();
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
        undoRedo.CreateAction("Set node callable");
        undoRedo.AddDoProperty(this, PropertyName.Target, new ValueFromNodePath(_popupSelectedNodePath));
        undoRedo.AddDoProperty(this, PropertyName.Method, _popupSelectedMethodName);
        undoRedo.AddDoMethod(this, GodotObject.MethodName.NotifyPropertyListChanged);
        undoRedo.AddUndoProperty(this, PropertyName.Target, Target);
        undoRedo.AddUndoProperty(this, PropertyName.Method, Method);
        undoRedo.AddUndoMethod(this, GodotObject.MethodName.NotifyPropertyListChanged);
        undoRedo.CommitAction();
    }
#endif
}