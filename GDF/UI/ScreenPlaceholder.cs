using GDF.Networking;
using Godot;
using Godot.Collections;

namespace GDF.UI;

[Icon($"{GdfConstants.IconRoot}/screen_placeholder.png")]
public partial class ScreenPlaceholder : Node
{
    [Export]
    public bool Visible
    {
        get => _uiRoot.Showing;
        set
        {
            if (value) ShowUI();
            else HideUI();
        }
    }

    public bool DeriveNameFromPath = false;

    // This node is NOT inside the tree unless Show is called
    private Screen _uiRoot;

    public ScreenPlaceholder()
    {
    }

    public ScreenPlaceholder(Screen ui)
    {
        _uiRoot = ui;
    }

    public bool IsVisible()
    {
        return Visible;
    }

    public bool SetVisible(bool visible)
    {
        return Visible = visible;
    }

    public void ShowUI()
    {
        _uiRoot.ShowUI();
    }

    public void HideUI()
    {
        _uiRoot.HideUI();
    }

    public void HideAndFreeUI()
    {
        _uiRoot.HideAndFreeUI();
    }

    public void FadeOutUI()
    {
        _uiRoot.FadeOutUI();
    }

    public void ForceFadeOutUI()
    {
        _uiRoot.ForceFadeOutUI();
    }

    public bool PrepareRootToEnterTree()
    {
        if (!IsInsideTree())
        {
            GD.PrintErr("Cannot show custom user interface when the user interface placeholder is not in the tree!");
            return false;
        }

        _uiRoot.OriginalNodePath = GetPath();
        if (DeriveNameFromPath)
            _uiRoot.Name = $"{Name} [{_uiRoot.OriginalNodePath.GetHashCode()}]";
        _uiRoot.Owner = null;
        return true;
    }

    [CustomRpc]
    public void CallUIMethod(StringName methodName)
    {
        _uiRoot.Call(methodName);
    }

    [CustomRpc]
    public void CallUIMethod(StringName methodName, Array args)
    {
        _uiRoot.Callv(methodName, args);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationExitTree)
            if (IsInstanceValid(_uiRoot))
                _uiRoot.CallDeferred(Screen.MethodName.ForceHideUI);
        if (what == NotificationPredelete)
            if (IsInstanceValid(_uiRoot))
                _uiRoot.QueueFree();
    }
}