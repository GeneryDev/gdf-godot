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
        get => _screen.Showing;
        set
        {
            if (value) ShowScreen();
            else HideScreen();
        }
    }

    public bool DeriveNameFromPath = false;

    // This node is NOT inside the tree unless Show is called
    private Screen _screen;

    public ScreenPlaceholder()
    {
    }

    public ScreenPlaceholder(Screen screen)
    {
        _screen = screen;
    }

    public bool IsVisible()
    {
        return Visible;
    }

    public bool SetVisible(bool visible)
    {
        return Visible = visible;
    }

    public void ShowScreen()
    {
        _screen.ShowScreen();
    }

    public void HideScreen()
    {
        _screen.HideScreen();
    }

    public void HideAndFreeScreen()
    {
        _screen.HideAndFreeScreen();
    }

    public void FadeOutScreen()
    {
        _screen.FadeOutScreen();
    }

    public void ForceFadeOutScreen()
    {
        _screen.ForceFadeOutScreen();
    }

    public bool PrepareScreenToEnterTree()
    {
        if (!IsInsideTree())
        {
            GD.PrintErr("Cannot show custom user interface when the user interface placeholder is not in the tree!");
            return false;
        }

        _screen.OriginalNodePath = GetPath();
        if (DeriveNameFromPath)
            _screen.Name = $"{Name} [{_screen.OriginalNodePath.GetHashCode()}]";
        _screen.Owner = null;
        return true;
    }

    [CustomRpc]
    public void CallScreenMethod(StringName methodName)
    {
        _screen.Call(methodName);
    }

    [CustomRpc]
    public void CallScreenMethod(StringName methodName, Array args)
    {
        _screen.Callv(methodName, args);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationExitTree)
            if (IsInstanceValid(_screen))
                _screen.CallDeferred(Screen.MethodName.ForceHideScreen);
        if (what == NotificationPredelete)
            if (IsInstanceValid(_screen))
                _screen.QueueFree();
    }
}