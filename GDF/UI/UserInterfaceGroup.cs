using System.Collections.Generic;
using GDF.Util;
using Godot;

namespace GDF.UI;

[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/user_interface_group.png")]
public partial class UserInterfaceGroup : Node
{
    public static readonly StringName GroupName = "user_interface_group";

    [Signal]
    public delegate void GroupFocusEnteredEventHandler();

    [Signal]
    public delegate void GroupFocusExitedEventHandler();

    [Signal]
    public delegate void ExclusiveGroupFocusEnteredEventHandler();

    [Signal]
    public delegate void ExclusiveGroupFocusExitedEventHandler();

    [Export] public bool RememberFocusStates = false;
    [Export] public bool ForgetFocusOnExitGroup = false;

    [Export] public UserInterface.NavigationInputType FocusableForInputTypes =
        UserInterface.NavigationInputType.ButtonsAndSticks |
        UserInterface.NavigationInputType.Mouse |
        UserInterface.NavigationInputType.Touch;

    [Export] public UserInterfaceGroup OverrideParentGroup;

    private UserInterface _focusInterface;
    private UserInterfaceGroup _parentGroup;
    private bool _focusInside = false;
    private bool _focusInsideExclusive = false;
    private Dictionary<int, UserInterfaceComponent> _rememberedFocusStates;

    public override void _EnterTree()
    {
        FindInterfaceAndParentGroup(out _focusInterface, out _parentGroup);
        if (_focusInterface != null)
        {
            _focusInterface.AddFocusableGroup(this);
            _focusInterface.PlayerFocusChanged += OnPlayerFocusChanged;
            _focusInterface.FocusableGroupChanged += OnInterfaceFocusableGroupChanged;
            _focusInside = HasFocus();
            _focusInsideExclusive = HasExclusiveFocus();
        }
        else
        {
            GD.PrintErr("No focusable interface for focusable group at: " + GetPath());
        }

        AddToGroup(GroupName);
    }

    public override void _ExitTree()
    {
        if (_focusInterface != null)
        {
            _focusInterface.RemoveFocusableGroup(this);
            _focusInterface.PlayerFocusChanged -= OnPlayerFocusChanged;
            _focusInterface.FocusableGroupChanged -= OnInterfaceFocusableGroupChanged;
        }
        _focusInterface = null;
    }

    private void OnInterfaceFocusableGroupChanged(UserInterfaceGroup from, UserInterfaceGroup to)
    {
        bool focusWasInside = _focusInside;
        bool focusNowInside = HasFocus();
        if (focusWasInside != focusNowInside)
        {
            _focusInside = focusNowInside;
            if (focusNowInside)
                EmitSignalGroupFocusEntered();
            else
                EmitSignalGroupFocusExited();
        }

        bool focusWasInsideExclusive = _focusInsideExclusive;
        bool focusNowInsideExclusive = HasExclusiveFocus();
        if (focusWasInsideExclusive != focusNowInsideExclusive)
        {
            _focusInsideExclusive = focusNowInsideExclusive;
            if (focusNowInsideExclusive)
                EmitSignalExclusiveGroupFocusEntered();
            else
                EmitSignalExclusiveGroupFocusExited();
        }
    }

    public UserInterface GetInterface()
    {
        return _focusInterface;
    }

    public bool FindInterfaceAndParentGroup(out UserInterface focusInterface,
        out UserInterfaceGroup focusableGroup)
    {
        focusInterface = null;
        focusableGroup = OverrideParentGroup;
        if (!IsInsideTree()) return false;

        var parent = GetParent();
        while (parent != null)
        {
            if (parent.GetChildOfType<UserInterfaceGroup>() is { } group && group != this && focusableGroup == null)
                focusableGroup = group;
            if (parent.GetChildOfType<UserInterface>() is { } @interface)
            {
                focusInterface = @interface;
                return true;
            }

            parent = parent.GetParent();
        }

        return false;
    }

    public void FocusGroup()
    {
        _focusInterface?.FocusGroup(this);
    }

    public void FocusFirstForAllPlayers()
    {
        var focusable = _focusInterface?.GetFirstValidFocusableInGroup(this);
        if (focusable != null) _focusInterface.FocusForAllPlayers(focusable);
    }

    public bool HasFocus()
    {
        return _focusInterface?.FocusedGroup == null || _focusInterface.FocusedGroup.IsInsideFocusableGroup(this);
    }

    public bool HasExclusiveFocus()
    {
        return _focusInterface?.FocusedGroup == this;
    }

    public bool IsInsideFocusableGroup(UserInterfaceGroup group)
    {
        var thisOrAncestor = this;
        while (thisOrAncestor != null)
        {
            if (thisOrAncestor == group) return true;
            thisOrAncestor = thisOrAncestor.GetParentGroup();
        }

        return false;
    }

    private void OnPlayerFocusChanged(int playerId, UserInterfaceComponent from, UserInterfaceComponent to)
    {
        if (!RememberFocusStates) return;
        if (!_focusInside) return;
        _rememberedFocusStates ??= new Dictionary<int, UserInterfaceComponent>();

        if (to?.GetFocusableGroup()?.IsInsideFocusableGroup(this) ?? false)
        {
            _rememberedFocusStates[playerId] = to;
            // GD.Print($"In Group {GetPath()}, focus for {playerId} is now {to?.GetPath()}");
        }
        else if(ForgetFocusOnExitGroup)
        {
            _rememberedFocusStates.Remove(playerId);
            // GD.Print($"In Group {GetPath()}, focus for {playerId} is now null");
        }
    }

    public void RestoreFocusStates()
    {
        if (_rememberedFocusStates == null) return;
        foreach ((int playerId, var focusable) in _rememberedFocusStates)
            if (IsInstanceValid(focusable) && (focusable.FocusableControl?.IsVisibleInTree() ?? false))
                _focusInterface?.Focus(playerId, focusable);
    }

    public UserInterfaceGroup GetParentGroup()
    {
        return _parentGroup;
    }
}