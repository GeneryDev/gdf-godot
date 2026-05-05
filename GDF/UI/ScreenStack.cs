using Godot;

namespace GDF.UI;

[Icon($"{GdfConstants.IconRoot}/screen_stack.png")]
public partial class ScreenStack : Control
{
    public static ScreenStack Instance
    {
        get
        {
            if (_instance == null)
                if (!Engine.IsEditorHint())
                    GD.PushError(
                        $"There's no singleton node instance for '{nameof(ScreenStack)}'. Make sure to add one to autoload.");
            if (Engine.IsEditorHint() && !IsInstanceValid(_instance)) _instance = null;
            return _instance;
        }
    }

    public static bool InstanceExists => _instance != null;

    private static ScreenStack _instance;

    public override void _EnterTree()
    {
        _instance = (ScreenStack)this;
    }

    public override void _ExitTree()
    {
        if (_instance == this && !Engine.IsEditorHint())
            _instance = null;
    }
    
    public override void _Ready()
    {
        base._Ready();
        ChildExitingTree += OnChildExitingTree;
    }

    public void Push(Screen ui)
    {
        var children = GetChildren();
        AddChild(ui);

        // Place after all other interfaces with order <= the incoming UI order.
        for (var index = 0; index < children.Count; index++)
        {
            var otherChild = children[index];
            if (otherChild is not Screen otherUI) continue;
            if (otherUI.GetEffectiveOrder() <= ui.GetEffectiveOrder()) continue;
            MoveChild(ui, index);
            break;
        }

        UpdateLayeredVisibility();
    }

    public void UpdateLayeredVisibility()
    {
        var shadowed = false;
        var children = GetChildren();
        for (int index = children.Count - 1; index >= 0; index--)
        {
            var node = children[index];
            if (node is not Screen otherUI) continue;

            if (otherUI.HideOnShadowed)
                otherUI.Visible = !shadowed;
            if (otherUI.Shadowed != shadowed)
            {
                otherUI.Shadowed = shadowed;
                if (shadowed)
                    otherUI.EmitSignal(Screen.SignalName.UIShadowingStarted);
                else
                    otherUI.EmitSignal(Screen.SignalName.UIShadowingEnded);
            }

            if (otherUI.ShadowLowerOrderInterfaces && otherUI.Showing) shadowed = true;
        }
    }

    private void OnChildExitingTree(Node node)
    {
        if (node is Screen ui)
        {
            ui.ControlFrame = ui.ControlFrame?.Remove();
            CallDeferred(MethodName.UpdateLayeredVisibility);
        }
    }
}