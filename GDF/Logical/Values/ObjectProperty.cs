using GDF.Editor;
using GDF.Util;
using Godot;

namespace GDF.Logical.Values;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/property.png")]
public partial class ObjectProperty : ValueSource
{
    [Export]
    public ValueSource Target;

    [Export]
    public StringName Property
    {
        get => _property;
        set
        {
            _property = value;
            _propertyPath = null;
        }
    }

    [Export] public bool Deep = false;
    
    [Export] public bool ErrorOnMissingTarget = true;
    
    private StringName _property;
    private NodePath _propertyPath;
    
    public Variant Get(Node source)
    {
        if (Target == null) return default;
        var targetObj = Target.GetValue(source).As<GodotObject>();
        if (targetObj == null)
        {
            MissingTargetPrintError(source);
            return default;
        }
        return Deep
            ? targetObj.GetIndexed(_propertyPath ??= new NodePath(Property))
            : targetObj.Get(Property);
    }
    public T Get<[MustBeVariant]T>(Node source)
    {
        return Get(source).As<T>();
    }

    public override Variant GetValue(Node source)
    {
        return Get(source);
    }

    public override string ToString()
    {
        return $"{Target} :: {Property}";
    }

    private void MissingTargetPrintError(Node source)
    {
        if (ErrorOnMissingTarget)
            GD.PrintErr($"Failed to execute {nameof(ObjectProperty)}, target returned null!\nIn: {(source?.IsInsideTree() ?? false ? source.GetPath() : source?.Name)}");
    }

#if TOOLS
    [InspectorCustomControl(AnchorProperty = nameof(Target), AnchorMode = InspectorPropertyAnchorMode.Before)]
    public Control SelectMethod()
    {
        var button = new Button();
        button.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        button.Icon = EditorInterface.Singleton.GetEditorTheme().GetIcon("Property", "EditorIcons");
        if (Target != null && Property is { IsEmpty: false })
            button.Text = this.ToString();
        else
            button.Text = "Select Property...";
        button.Connect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.ShowPicker));
        
        return button;
    }

    private void ShowPicker()
    {
        EditorUtils.ShowNodeAndPropertyPicker(EditorInterface.Singleton.GetInspector().GetEditedObject() as Node, PickerCallback);
    }

    private void PickerCallback(NodePath nodePath, string propertyPath)
    {
        var undoRedo = EditorInterface.Singleton.GetEditorUndoRedo();
        undoRedo.CreateAction("Set node property");
        undoRedo.AddDoProperty(this, PropertyName.Target, new ValueFromNodePath(nodePath));
        undoRedo.AddDoProperty(this, PropertyName.Property, propertyPath);
        undoRedo.AddDoMethod(this, GodotObject.MethodName.NotifyPropertyListChanged);
        undoRedo.AddUndoProperty(this, PropertyName.Target, Target);
        undoRedo.AddUndoProperty(this, PropertyName.Property, Property);
        undoRedo.AddUndoMethod(this, GodotObject.MethodName.NotifyPropertyListChanged);
        undoRedo.CommitAction();
    }
#endif
}