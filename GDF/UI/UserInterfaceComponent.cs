using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using GDF.Input;
using GDF.Logical.Signals;
using GDF.Logical.Values;
using GDF.Multiplayer;
using GDF.Util;
using Godot;
using Godot.Collections;

namespace GDF.UI;

[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/user_interface_component.png")]
public sealed partial class UserInterfaceComponent : Node, IInboundArgumentSource
{
    public static readonly StringName GroupName = "ui_component";

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

    [Export] public bool Submittable = false;
    
    [ExportGroup("Navigable To")]
    [Export(PropertyHint.GroupEnable)]
    public bool NavigableTo
    {
        get => _navigableTo;
        set
        {
            _navigableTo = value;
            UpdateFocusable();
        }
    }
    [Export] public NavigabilityConditionEnum NavigabilityCondition = NavigabilityConditionEnum.WhenGroupHasFocus;
    [Export] public int AutoFocusPriority = 0;
    
    [ExportGroup("Clickable")]
    [Export(PropertyHint.GroupEnable)] public bool Clickable = false;
    [Export] public ClickModeEnum ClickMode = ClickModeEnum.SubmitOnly;
    
    [ExportGroup("Tappable")]
    [Export(PropertyHint.GroupEnable)] public bool Tappable = false;
    [Export] public TapModeEnum TapMode = TapModeEnum.SubmitOnly;

    [ExportGroup("Consumes Navigation")]
    [Export(PropertyHint.GroupEnable)] public bool ConsumesNavigation = false;
    [Export] public Godot.Collections.Dictionary<Side, ObjectCallable> ConsumedNavigationSides = new();
    
    [ExportGroup("Focused Actions")]
    [Export(PropertyHint.GroupEnable)] public bool FocusedActionsEnabled = false;
    [Export] public Godot.Collections.Dictionary<GdfInputAction, ObjectCallable> FocusedActions = new();

    [ExportGroup("Shortcut")]
    [Export(PropertyHint.GroupEnable)] public bool ShortcutEnabled = false;
    [Export] public GdfInputAction ShortcutAction;
    [Export] public ShortcutConditionEnum ShortcutCondition = ShortcutConditionEnum.WhenGroupHasFocus;
    [Export] public ShortcutModeEnum ShortcutMode = ShortcutModeEnum.SubmitOnly;

    [ExportGroup("UX")]
    [Export] public Control OverrideOutlinedControl;
    [Export] public UserInterfaceGroup OverrideParentGroup;
    [Export] public bool EmulateButtonHover = false;
    [Export] public bool DisableBuiltInFocus = true;
    
    [ExportGroup("Texts")]
    [Export] public bool ShowSubmitContextAction = true;
    [Export] public bool ShowFocusedActionContextAction = true;
    [Export] public bool ShowShortcutContextAction = true;
    [Export] public string OverrideSubmitText = null;
    [Export] public string OverrideShortcutText = null;
    [Export] public Godot.Collections.Dictionary<GdfInputAction, string> OverrideFocusedActionTexts = new();

    public Control OutlinedControl => OverrideOutlinedControl ?? GetParent<Control>();

    private bool _navigableTo = false;
    private bool _groupHasFocus = true;
    private bool _groupHasExclusiveFocus = true;
    private UserInterface _ui;
    private UserInterfaceGroup _uiGroup;
    private bool _wasFocusedBeforePress = false;
    private List<int> _playersEnabledShortcut;
    private List<int> _focusedPlayers;
    private int _callableArgPlayerId = -1;

    public Control FocusableControl;

    public bool HasShortcut => ShortcutAction != null;

    public override void _Ready()
    {
        FocusableControl = GetParent<Control>();

        var parent = GetParent<Control>();
        if (parent == null) return;

        UpdateFocusable();

        if (FocusableControl != null)
        {
            FocusableControl.FocusEntered += OnBuiltInFocusEntered;
            FocusableControl.GuiInput += OnGuiInput;
        }
    }

    public override void _EnterTree()
    {
        FindInterfaceAndGroup(out _ui, out _uiGroup);
        EmitSignalDataContextUpdated();
        _ui?.AddComponent(this);
        if (_uiGroup != null)
        {
            _uiGroup.GroupFocusEntered += OnGroupFocusEntered;
            _uiGroup.GroupFocusExited += OnGroupFocusExited;
            _uiGroup.ExclusiveGroupFocusEntered += OnGroupExclusiveFocusEntered;
            _uiGroup.ExclusiveGroupFocusExited += OnGroupExclusiveFocusExited;
            if (_uiGroup.HasFocus()) OnGroupFocusEntered();
            else OnGroupFocusExited();
            if (_uiGroup.HasExclusiveFocus()) OnGroupExclusiveFocusEntered();
            else OnGroupExclusiveFocusExited();
        }

        if (_ui == null) GD.PrintErr("No user interface for component at: " + GetPath());
        AddToGroup(GroupName);
    }

    public override void _ExitTree()
    {
        if (_uiGroup != null)
        {
            _uiGroup.GroupFocusEntered -= OnGroupFocusEntered;
            _uiGroup.GroupFocusExited -= OnGroupFocusExited;
            _uiGroup.ExclusiveGroupFocusEntered -= OnGroupExclusiveFocusEntered;
            _uiGroup.ExclusiveGroupFocusExited -= OnGroupExclusiveFocusExited;
        }

        _uiGroup = null;

        _ui?.RemoveComponent(this);
        _ui?.ComponentLoseFocus(this);
        _ui = null;
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
        if (!(_ui?.HasControl() ?? false)) return;
        if (Clickable && evt is InputEventMouseButton { ButtonIndex: MouseButton.Left } mEvt)
        {
            if (evt.IsPressed())
            {
                if (ClickMode is ClickModeEnum.FocusOnly or ClickModeEnum.OnceFocusAndSubmit
                    or ClickModeEnum.OnceFocusTwiceSubmit)
                    PressedDirectly(false, UserInterface.NavigationInputType.Mouse, ClickMode);
            }
            else if (FocusableControl.GetGlobalRect().HasPoint(mEvt.GlobalPosition))
            {
                if (ClickMode is ClickModeEnum.OnceFocusAndSubmit or ClickModeEnum.SubmitOnly
                    or ClickModeEnum.OnceFocusTwiceSubmit)
                    PressedDirectly(true, UserInterface.NavigationInputType.Mouse, ClickMode);
            }
        }
        else if (Tappable && evt is InputEventScreenTouch tEvt)
        {
            if (evt.IsPressed())
            {
                if (TapMode is TapModeEnum.FocusOnly or TapModeEnum.OnceFocusAndSubmit
                    or TapModeEnum.OnceFocusTwiceSubmit)
                    PressedDirectly(false, UserInterface.NavigationInputType.Touch, (ClickModeEnum)(int)TapMode);
            }
            else if (FocusableControl.GetGlobalRect().HasPoint(tEvt.Position))
            {
                if (TapMode is TapModeEnum.OnceFocusAndSubmit or TapModeEnum.SubmitOnly
                    or TapModeEnum.OnceFocusTwiceSubmit)
                    PressedDirectly(true, UserInterface.NavigationInputType.Touch, (ClickModeEnum)(int)TapMode);
            }
        }
    }

    private void PressedDirectly(bool isRelease, UserInterface.NavigationInputType type, ClickModeEnum mode)
    {
        var focusInterface = GetUserInterface();
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
                    if (playerId != -1 && !(Room.Instance.GetPlayerInfo(playerId)?.IsLocal ?? false))
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
                _ui?.UpdateInputType(type);
                if (mode != ClickModeEnum.SubmitOnly) focusInterface.Focus(playerId, this);
                if (isRelease && (mode is ClickModeEnum.OnceFocusAndSubmit or ClickModeEnum.SubmitOnly ||
                                  (mode is ClickModeEnum.OnceFocusTwiceSubmit && _wasFocusedBeforePress)))
                    Submit(playerId);
            }
        }
    }

    private void OnBuiltInFocusEntered()
    {
        if (DisableBuiltInFocus && FocusableControl.HasFocus()) FocusableControl.ReleaseFocus();
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

    private void TrackFocusEnter(int playerId)
    {
        _focusedPlayers ??= new();
        if (!_focusedPlayers.Contains(playerId))
        {
            _focusedPlayers.Add(playerId);
            TrackedFocusChanged();
        }
    }

    private void TrackFocusExit(int playerId)
    {
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

    public bool IsAnyPlayerFocused()
    {
        return _focusedPlayers is {Count: > 0};
    }

    public bool IsPlayerFocused(int playerId)
    {
        return _focusedPlayers?.Contains(playerId) ?? false;
    }

    public void FocusGroup()
    {
        _ui?.FocusGroup(_uiGroup);
    }

    public void Focus(int playerId)
    {
        _ui?.Focus(playerId, this);
    }

    public void FocusForAllPlayers()
    {
        _ui?.FocusForAllPlayers(this);
    }

    public void Submit(int playerId)
    {
        if (!Submittable) return;
        EmitSignal(SignalName.PlayerSubmitted, playerId);
        EmitSignal(SignalName.Submitted);
        //GD.Print($"Player {playerId} pressed Submit on {GetParent()?.GetPath()}");
    }

    public void SubmitFocusedAction(int playerId, GdfInputAction action)
    {
        if (!Submittable) return;
        var callable = FocusedActions?.GetValueOrDefault(action);
        InvokeCallable(callable, playerId);
        
        //GD.Print($"Player {playerId} pressed [{action.DisplayName}] on {GetParent()?.GetPath()}");
    }

    private void OnControlVisibilityChanged()
    {
        if (FocusableControl != null && !FocusableControl.IsVisibleInTree())
            GetUserInterface()?.CallDeferred(UserInterface.MethodName.ComponentLoseFocus, this);
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

    public UserInterface GetUserInterface()
    {
        return _ui;
    }

    public UserInterfaceGroup GetUserInterfaceGroup()
    {
        return _uiGroup;
    }

    public bool IsGroupFocused()
    {
        return _groupHasFocus;
    }

    public bool IsGroupExclusivelyFocused()
    {
        return _groupHasExclusiveFocus;
    }

    public bool FindInterfaceAndGroup(out UserInterface ui, out UserInterfaceGroup uiGroup)
    {
        ui = null;
        uiGroup = OverrideParentGroup;
        if (!IsInsideTree()) return false;

        var parent = GetParent();
        while (parent != null)
        {
            if (parent.GetChildOfType<UserInterfaceGroup>() is { } group && uiGroup == null)
                uiGroup = group;
            if (parent.GetChildOfType<UserInterface>() is { } @interface)
            {
                ui = @interface;
                return true;
            }

            parent = parent.GetParent();
        }

        return false;
    }

    public bool ConsumeNavigation(int playerId, Side side)
    {
        if (!ConsumesNavigation) return false;

        if (ConsumedNavigationSides?.TryGetValue(side, out var callable) ?? false)
        {
            EmitSignal(SignalName.ConsumedNavigation, Variant.From(side));
            EmitSignal(SignalName.PlayerConsumedNavigation, playerId, Variant.From(side));

            InvokeCallable(callable, playerId);

            return true;
        }

        return false;
    }

    public void HandleFocusedActions(int playerId, GdfPlayerInput input)
    {
        if (!FocusedActionsEnabled) return;
        if (FocusedActions == null) return;
        foreach (var (action, callable) in FocusedActions)
            if (input.ConsumeActionEvent(action))
            {
                _ui?.UpdateInputType(UserInterface.NavigationInputType.ButtonsAndSticks);
                SubmitFocusedAction(playerId, action);
            }
    }

    public bool IsShortcutEnabled(int playerId)
    {
        if (FocusableControl == null || !FocusableControl.IsVisibleInTree()) return false;
        if (!ShortcutEnabled) return false;
        switch (ShortcutCondition)
        {
            case ShortcutConditionEnum.WhenGroupHasFocus:
                return GetUserInterfaceGroup() is not { } group1 || group1.HasFocus();
            case ShortcutConditionEnum.WhenGroupHasExclusiveFocus:
                return GetUserInterfaceGroup() is not { } group2 || group2.HasExclusiveFocus();
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
        return _ui?.AnyPlayerHasFocus(this) ?? false;
    }

    public void UpdateInputType(UserInterface.NavigationInputType type)
    {
        _ui?.UpdateInputType(type);
    }

    private void InvokeCallable(ObjectCallable callable, int playerId)
    {
        if (callable == null) return;
        var args = callable.EvaluateArgs(this);
        if (args.Length > 0) args[0] = playerId;
        callable.CallWithArgs(this, args);
    }

    public Variant GetArgument(int index)
    {
        if (index == 0) return _callableArgPlayerId;
        return default;
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

    public enum TapModeEnum
    {
        FocusOnly,
        OnceFocusTwiceSubmit,
        OnceFocusAndSubmit,
        SubmitOnly
    }
}