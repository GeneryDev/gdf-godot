using Godot;

namespace GDF.UI;

[GlobalClass]
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

    public void Push(Screen screen)
    {
        if (screen == null) return;
        var children = GetChildren();
        var layer = screen.GetOrCreateLayer();
        AddChild(layer);

        // Place after all other interfaces with order <= the incoming Screen order.
        for (var index = 0; index < children.Count; index++)
        {
            var otherChild = children[index];
            if (otherChild is not CanvasLayer otherScreenLayer) continue;
            if (otherScreenLayer.Layer <= screen.GetEffectiveOrder()) continue;
            MoveChild(screen, index);
            break;
        }

        UpdateLayeredVisibility();
    }

    public void Remove(Screen screen)
    {
        if (screen == null) return;
        if (screen.GetParent() == this)
        {
            this.RemoveChild(screen);
        }
        else if (screen.GetParent() is CanvasLayer screenLayer && screenLayer.GetParent() == this)
        {
            this.RemoveChild(screenLayer);
        }
    }

    public void UpdateLayeredVisibility()
    {
        var shadowed = false;
        var children = GetChildren();
        for (int index = children.Count - 1; index >= 0; index--)
        {
            var node = children[index];
            if (node is not Screen otherScreen) continue;

            if (otherScreen.HideOnShadowed)
                otherScreen.Visible = !shadowed;
            if (otherScreen.Shadowed != shadowed)
            {
                otherScreen.Shadowed = shadowed;
                if (shadowed)
                    otherScreen.EmitSignal(Screen.SignalName.ScreenShadowingStarted);
                else
                    otherScreen.EmitSignal(Screen.SignalName.ScreenShadowingEnded);
            }

            if (otherScreen.ShadowLowerOrderInterfaces && otherScreen.Showing) shadowed = true;
        }
    }

    private void OnChildExitingTree(Node node)
    {
        if (node is Screen or CanvasLayer)
        {
            CallDeferred(MethodName.UpdateLayeredVisibility);
        }
    }
}