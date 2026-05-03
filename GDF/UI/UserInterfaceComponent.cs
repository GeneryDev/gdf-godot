using System.Collections.Generic;
using GDF.Input;
using GDF.Logical.Values;
using GDF.Multiplayer;
using GDF.Util;
using Godot;
using Godot.Collections;

namespace GDF.UI;

[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/user_interface_component.png")]
public sealed partial class UserInterfaceComponent : Node
{
    public static readonly StringName GroupName = "player_focusable";

    [Signal]
    public delegate void FocusEnteredEventHandler();

    [Signal]
    public delegate void PlayerFocusEnteredEventHandler(int playerId);

    [Signal]
    public delegate void FocusExitedEventHandler();

    [Signal]
    public delegate void PlayerFocusExitedEventHandler(int playerId);

    [Signal]
    public delegate void SubmittedEventHandler();

    [Signal]
    public delegate void PlayerSubmittedEventHandler(int playerId);

    [Signal]
    public delegate void ConsumedNavigationEventHandler(Side side);

    [Signal]
    public delegate void PlayerConsumedNavigationEventHandler(int playerId, Side side);

    [Signal]
    public delegate void GroupFocusEnteredEventHandler();

    [Signal]
    public delegate void GroupFocusExitedEventHandler();

    [Signal]
    public delegate void GroupExclusiveFocusEnteredEventHandler();

    [Signal]
    public delegate void GroupExclusiveFocusExitedEventHandler();

    [Export]
    public bool NavigableTo
    {
        get => _navigableTo;
        set
        {
            _navigableTo = value;
            UpdateFocusable();
        }
    }

    [Export] public bool Submittable = true;

    [Export] public bool DisableTrueFocus = true;
    [Export] public Control OverrideOutlinedControl;
    [Export] public UserInterfaceGroup OverrideParentGroup;
    [ExportGroup("Input")]
    [Export] public Array<Side> ConsumedNavigationSides = new();
    [Export] public Godot.Collections.Dictionary<GdfInputAction, ObjectCallable> SubActions;
    [Export] public bool Clickable = true;
    [Export] public ClickModeEnum ClickMode = ClickModeEnum.OnceFocusAndSubmit;

    [ExportGroup("Shortcut")]
    [Export] public GdfInputAction ShortcutAction;
    [Export] public ShortcutConditionEnum ShortcutCondition = ShortcutConditionEnum.WhenGroupHasFocus;
    [Export] public ShortcutModeEnum ShortcutMode = ShortcutModeEnum.OnceFocusTwiceSubmit;

    [ExportGroup("UX")]
    [Export] public int AutoFocusPriority = 0;
    [Export] public bool EmulateButtonHover = false;
    [Export] public bool ShowSubmitContextAction = true;
    [Export] public bool ShowSubActionContextAction = true;
    [Export] public bool ShowShortcutContextAction = true;
    [Export] public NavigabilityConditionEnum NavigabilityCondition = NavigabilityConditionEnum.WhenGroupHasFocus;
    [Export] public string OverrideSubmitText = null;
    [Export] public string OverrideShortcutText = null;
    [Export] public Godot.Collections.Dictionary<GdfInputAction, string> OverrideSubActionTexts = null;

    public Control OutlinedControl => OverrideOutlinedControl ?? GetParent<Control>();

    private bool _navigableTo = true;
    private bool _groupHasFocus = true;
    private bool _groupHasExclusiveFocus = true;
    private UserInterface _focusInterface;
    private UserInterfaceGroup _focusableGroup;
    private bool _wasFocusedBeforePress = false;
    private List<int> _playersEnabledShortcut;
    private List<int> _focusedPlayers;

    public Control FocusableControl;

    public bool HasShortcut => ShortcutAction != null;

    public override void _Ready()
    {
        ConsumedNavigationSides = ConsumedNavigationSides?.Duplicate();

        FocusableControl = GetParent<Control>();

        var parent = GetParent<Control>();
        if (parent == null) return;

        UpdateFocusable();

        if (FocusableControl != null)
        {
            FocusableControl.FocusEntered += OnTrueFocusEntered;
            FocusableControl.GuiInput += OnGuiInput;
        }
    }

    public override void _EnterTree()
    {
        FindInterfaceAndGroup(out _focusInterface, out _focusableGroup);
        EmitSignalDataContextUpdated();
        _focusInterface?.AddFocusable(this);
        if (_focusableGroup != null)
        {
            _focusableGroup.GroupFocusEntered += OnGroupFocusEntered;
            _focusableGroup.GroupFocusExited += OnGroupFocusExited;
            _focusableGroup.ExclusiveGroupFocusEntered += OnGroupExclusiveFocusEntered;
            _focusableGroup.ExclusiveGroupFocusExited += OnGroupExclusiveFocusExited;
            if (_focusableGroup.HasFocus()) OnGroupFocusEntered();
            else OnGroupFocusExited();
            if (_focusableGroup.HasExclusiveFocus()) OnGroupExclusiveFocusEntered();
            else OnGroupExclusiveFocusExited();
        }

        if (_focusInterface == null) GD.PrintErr("No focusable interface for focusable at: " + GetPath());
        AddToGroup(GroupName);
    }

    public override void _ExitTree()
    {
        if (_focusableGroup != null)
        {
            _focusableGroup.GroupFocusEntered -= OnGroupFocusEntered;
            _focusableGroup.GroupFocusExited -= OnGroupFocusExited;
            _focusableGroup.ExclusiveGroupFocusEntered -= OnGroupExclusiveFocusEntered;
            _focusableGroup.ExclusiveGroupFocusExited -= OnGroupExclusiveFocusExited;
        }

        _focusableGroup = null;

        _focusInterface?.RemoveFocusable(this);
        _focusInterface?.FocusableLoseFocus(this);
        _focusInterface = null;
    }

    private void OnGroupFocusEntered()
    {
        _groupHasFocus = true;
        UpdateFocusable();
        EmitSignalGroupFocusEntered();
    }

    private void OnGroupFocusExited()
    {
        _groupHasFocus = false;
        UpdateFocusable();
        EmitSignalGroupFocusExited();
    }

    private void OnGroupExclusiveFocusEntered()
    {
        _groupHasExclusiveFocus = true;
        UpdateFocusable();
        EmitSignalGroupExclusiveFocusEntered();
    }

    private void OnGroupExclusiveFocusExited()
    {
        _groupHasExclusiveFocus = false;
        UpdateFocusable();
        EmitSignalGroupExclusiveFocusExited();
    }

    private void OnGuiInput(InputEvent evt)
    {
        if (!Clickable || !(_focusInterface?.HasControl() ?? false)) return;
        if (evt is InputEventMouseButton { ButtonIndex: MouseButton.Left } mEvt)
        {
            if (evt.IsPressed())
            {
                if (ClickMode is ClickModeEnum.FocusOnly or ClickModeEnum.OnceFocusAndSubmit
                    or ClickModeEnum.OnceFocusTwiceSubmit)
                    PressedDirectly(false, UserInterface.NavigationInputType.Mouse);
            }
            else if (FocusableControl.GetGlobalRect().HasPoint(mEvt.GlobalPosition))
            {
                if (ClickMode is ClickModeEnum.OnceFocusAndSubmit or ClickModeEnum.SubmitOnly
                    or ClickModeEnum.OnceFocusTwiceSubmit)
                    PressedDirectly(true, UserInterface.NavigationInputType.Mouse);
            }
        }
        else if (evt is InputEventScreenTouch tEvt)
        {
            if (evt.IsPressed())
            {
                if (ClickMode is ClickModeEnum.FocusOnly or ClickModeEnum.OnceFocusAndSubmit
                    or ClickModeEnum.OnceFocusTwiceSubmit)
                    PressedDirectly(false, UserInterface.NavigationInputType.Touch);
            }
            else if (FocusableControl.GetGlobalRect().HasPoint(tEvt.Position))
            {
                if (ClickMode is ClickModeEnum.OnceFocusAndSubmit or ClickModeEnum.SubmitOnly
                    or ClickModeEnum.OnceFocusTwiceSubmit)
                    PressedDirectly(true, UserInterface.NavigationInputType.Touch);
            }
        }
    }

    private void PressedDirectly(bool isRelease, UserInterface.NavigationInputType type)
    {
        var focusInterface = GetInterface();
        if (focusInterface is { ClickToFocus: true })
        {
            int playerId = -1;

            switch (focusInterface.Operability)
            {
                case UserInterface.OperabilityEnum.AllPlayers:
                    playerId = Room.Instance.GetOnlyLocalPlayerId();
                    break;
                case UserInterface.OperabilityEnum.SpecificPlayer:
                    playerId = focusInterface.ExclusiveToPlayerId;
                    if (playerId == -1)
                        playerId = Room.Instance.GetOnlyLocalPlayerId();
                    if (playerId != -1 && !(Room.Instance.GetPlayerInfo(playerId)?.OwnedByThisClient ?? false))
                        playerId = -1;
                    break;
                case UserInterface.OperabilityEnum.Playerless:
                case UserInterface.OperabilityEnum.PlayerlessAuthority:
                    playerId = UserInterface.PlayerlessId;
                    break;
            }

            if (!isRelease)
                _wasFocusedBeforePress = focusInterface.GetPlayerFocus(playerId) == this;

            if (playerId != -1 || focusInterface.Operability is UserInterface.OperabilityEnum.Playerless or UserInterface.OperabilityEnum.PlayerlessAuthority)
            {
                _focusInterface?.UpdateInputType(type);
                if (ClickMode != ClickModeEnum.SubmitOnly) focusInterface.Focus(playerId, this);
                if (isRelease && (ClickMode is ClickModeEnum.OnceFocusAndSubmit or ClickModeEnum.SubmitOnly ||
                                  (ClickMode is ClickModeEnum.OnceFocusTwiceSubmit && _wasFocusedBeforePress)))
                    Submit(playerId);
            }
        }
    }

    private void OnTrueFocusEntered()
    {
        if (DisableTrueFocus && FocusableControl.HasFocus()) FocusableControl.ReleaseFocus();
    }

    public override void _Notification(int what)
    {
        if (Engine.IsEditorHint()) return;
        if (what == NotificationParented)
        {
            FocusableControl = GetParent<Control>();
            if (FocusableControl != null) FocusableControl.VisibilityChanged += OnControlVisibilityChanged;
        }
        else if (what == NotificationUnparented)
        {
            if (FocusableControl != null) FocusableControl.VisibilityChanged -= OnControlVisibilityChanged;
            FocusableControl = null;
        }
    }

    public void EnterFocus(int playerId)
    {
        TrackFocusEnter(playerId);
        EmitSignal(SignalName.FocusEntered);
        EmitSignal(SignalName.PlayerFocusEntered, playerId);
    }

    public void ExitFocus(int playerId)
    {
        TrackFocusExit(playerId);
        EmitSignal(SignalName.FocusExited);
        EmitSignal(SignalName.PlayerFocusExited, playerId);
    }

    private bool ShouldTrackFocus()
    {
        return EmulateButtonHover;
    }

    private void TrackFocusEnter(int playerId)
    {
        if (!ShouldTrackFocus()) return;
        _focusedPlayers ??= new();
        if (!_focusedPlayers.Contains(playerId))
        {
            _focusedPlayers.Add(playerId);
            TrackedFocusChanged();
        }
    }

    private void TrackFocusExit(int playerId)
    {
        if (!ShouldTrackFocus()) return;
        if (_focusedPlayers?.Remove(playerId) ?? false)
        {
            TrackedFocusChanged();
        }
    }

    private void TrackedFocusChanged()
    {
        if (EmulateButtonHover && FocusableControl is Button btn)
        {
            var normalStyleboxName = "normal";
            if (_focusedPlayers is { Count: > 0 })
            {
                btn.AddThemeStyleboxOverride(normalStyleboxName, btn.GetThemeStylebox("hover"));
            }
            else
            {
                btn.RemoveThemeStyleboxOverride(normalStyleboxName);
            }
        }
    }

    public void FocusGroup()
    {
        _focusInterface?.FocusGroup(_focusableGroup);
    }

    public void Focus(int playerId)
    {
        _focusInterface?.Focus(playerId, this);
    }

    public void FocusForAllPlayers()
    {
        _focusInterface?.FocusForAllPlayers(this);
    }

    public void Submit(int playerId)
    {
        if (!Submittable) return;
        EmitSignal(SignalName.PlayerSubmitted, playerId);
        EmitSignal(SignalName.Submitted);
        GD.Print($"Player {playerId} pressed Submit on {GetParent()?.GetPath()}");
    }

    public void SubmitSubAction(int playerId, GdfInputAction action)
    {
        if (!Submittable) return;
        var callable = SubActions?.GetValueOrDefault(action);
        if (callable == null) return;
        var args = callable.EvaluateArgs(this);
        if (args.Length > 0) args[0] = playerId;
        callable.CallWithArgs(this, args);
        GD.Print($"Player {playerId} pressed [{action.DisplayName}] on {GetParent()?.GetPath()}");
    }

    private void OnControlVisibilityChanged()
    {
        if (FocusableControl != null && !FocusableControl.IsVisibleInTree())
            GetInterface()?.CallDeferred(UserInterface.MethodName.FocusableLoseFocus, this);
    }

    private void UpdateFocusable()
    {
        if (FocusableControl != null)
            FocusableControl.FocusMode =
                (_navigableTo && (NavigabilityCondition switch
                {
                    NavigabilityConditionEnum.Always => true,
                    NavigabilityConditionEnum.WhenGroupHasFocus => _groupHasFocus,
                    NavigabilityConditionEnum.WhenGroupHasExclusiveFocus => _groupHasExclusiveFocus,
                    _ => true
                })) ? Control.FocusModeEnum.All : Control.FocusModeEnum.None;
    }

    public bool NavigabilityConditionMet()
    {
        return FocusableControl?.FocusMode == Control.FocusModeEnum.All;
    }

    public UserInterface GetInterface()
    {
        return _focusInterface;
    }

    public UserInterfaceGroup GetFocusableGroup()
    {
        return _focusableGroup;
    }

    public bool IsGroupFocused()
    {
        return _groupHasFocus;
    }

    public bool IsGroupExclusivelyFocused()
    {
        return _groupHasExclusiveFocus;
    }

    public bool FindInterfaceAndGroup(out UserInterface focusInterface, out UserInterfaceGroup focusableGroup)
    {
        focusInterface = null;
        focusableGroup = OverrideParentGroup;
        if (!IsInsideTree()) return false;

        var parent = GetParent();
        while (parent != null)
        {
            if (parent.GetChildOfType<UserInterfaceGroup>() is { } group && focusableGroup == null)
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

    public bool ConsumeNavigation(int playerId, Side side)
    {
        foreach (var consumedSide in ConsumedNavigationSides)
            if (consumedSide == side)
            {
                EmitSignal(SignalName.ConsumedNavigation, Variant.From(side));
                EmitSignal(SignalName.PlayerConsumedNavigation, playerId, Variant.From(side));
                return true;
            }

        return false;
    }

    public void HandleSubActions(int playerId, GdfPlayerInput input)
    {
        if (SubActions == null) return;
        foreach (var (action, callable) in SubActions)
            if (input.ConsumeActionEvent(action))
            {
                _focusInterface?.UpdateInputType(UserInterface.NavigationInputType.ButtonsAndSticks);
                SubmitSubAction(playerId, action);
            }
    }

    public bool IsShortcutEnabled(int playerId)
    {
        if (FocusableControl == null || !FocusableControl.IsVisibleInTree()) return false;
        switch (ShortcutCondition)
        {
            case ShortcutConditionEnum.WhenGroupHasFocus:
                return GetFocusableGroup() is not { } group1 || group1.HasFocus();
            case ShortcutConditionEnum.WhenGroupHasExclusiveFocus:
                return GetFocusableGroup() is not { } group2 || group2.HasExclusiveFocus();
            case ShortcutConditionEnum.PerPlayerCondition:
                return _playersEnabledShortcut is { } enabledList && enabledList.Contains(playerId);
            case ShortcutConditionEnum.Always:
                return true;
            case ShortcutConditionEnum.Never:
                return false;
            default:
                return true;
        }
    }

    public void EnablePlayerShortcut(int playerId)
    {
        _playersEnabledShortcut ??= new();
        if (!_playersEnabledShortcut.Contains(playerId))
            _playersEnabledShortcut.Add(playerId);
    }

    public void DisablePlayerShortcut(int playerId)
    {
        _playersEnabledShortcut ??= new();
        _playersEnabledShortcut.Remove(playerId);
    }

    public bool AnyPlayerHasFocus()
    {
        return _focusInterface?.AnyPlayerHasFocus(this) ?? false;
    }

    public void UpdateInputType(UserInterface.NavigationInputType type)
    {
        _focusInterface?.UpdateInputType(type);
    }

    public enum ShortcutModeEnum
    {
        FocusOnly,
        OnceFocusTwiceSubmit,
        OnceFocusAndSubmit,
        SubmitOnly
    }

    public enum NavigabilityConditionEnum
    {
        WhenGroupHasFocus,
        WhenGroupHasExclusiveFocus,
        Always
    }

    public enum ShortcutConditionEnum
    {
        WhenGroupHasFocus = 0,
        WhenGroupHasExclusiveFocus = 1,
        PerPlayerCondition = 3,
        Always = 2,
        Never = 4
    }

    public enum ClickModeEnum
    {
        FocusOnly,
        OnceFocusTwiceSubmit,
        OnceFocusAndSubmit,
        SubmitOnly
    }
}