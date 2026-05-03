using System;
using System.Collections.Generic;
using GDF.Input;
using GDF.Multiplayer;
using GDF.Networking;
using GDF.PropertyStacks;
using GDF.PropertyStacks.Extensions;
using GDF.Util;
using Godot;
using Systems.Inputs;

namespace GDF.UI;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/user_interface.png")]
public partial class UserInterface : Node, IResynchronizable
{
    public static readonly StringName Group = "user_interface";
    public const int PlayerlessId = -1;

    public static event Action GlobalLastUsedInputTypeChanged;
    
    [Signal]
    public delegate void PlayerFocusChangedEventHandler(int playerId, UserInterfaceComponent from, UserInterfaceComponent to);

    [Signal]
    public delegate void FocusableGroupChangedEventHandler(UserInterfaceGroup from, UserInterfaceGroup to);

    [Signal]
    public delegate void InputTypeUpdatedEventHandler();

    [Export] public PackedScene FocusVisualTemplate;
    [Export] public bool AutoFocusFirst = false;
    [Export] public bool UnfocusOnReenter = true;
    [Export] public OperabilityEnum Operability = OperabilityEnum.Playerless;

    [ExportGroup("Input")]
    [Export(PropertyHint.ResourceType, nameof(GdfInputAutoConfiguration))]
    public Resource InputAutoConfig;

    [Export] public GdfInputAction NavigateAction;
    [Export] public Vector3 NavigateRightVec = new(1, 0, 0);
    [Export] public Vector3 NavigateDownVec = new(0, 1, 0);
    [Export] public GdfInputAction SubmitAction;
    [Export] public bool ClickToFocus = true;

    [ExportGroup("UX")]
    [Export] public bool ShowNavigateContextAction = false;
    [Export] public string OverrideNavigateText = null;
    [Export] public string OverrideSubmitText = null;

    [ExportGroup("Warnings")]
    [Export] public bool SuppressOperabilityWarnings = false;

    [ExportGroup("Networking")]
    [Export] public bool ReplicateToPeers = true;

    public int ExclusiveToPlayerId = -1;
    public PropertyFrame RequireFrameControl;
    public string RequireFrameControlPropertyId = InputGroups.Default;

    private Dictionary<int, PlayerFocusState> _playerFocusStates = new();
    private Dictionary<int, PlayerInputState> _playerInputStates = new();
    private List<UserInterfaceComponent> _focusables = new();
    public UserInterfaceGroup FocusedGroup { get; private set; }
    private UserInterfaceGroup _lastRequestedFocusedGroup = null;
    public static NavigationInputType GlobalLastUsedInputType { get; private set; } = 0;
    public NavigationInputType LastUsedInputType => GlobalLastUsedInputType;


    public override void _Ready()
    {
        if (UnfocusOnReenter)
            UnfocusAll();
        if (!Engine.IsEditorHint() && Operability == OperabilityEnum.SpecificPlayer && ExclusiveToPlayerId == -1 &&
            !SuppressOperabilityWarnings)
            GD.PushWarning(
                $"Focus Interface Operability set to SpecificPlayer, but no player ID was set in ExclusiveToPlayerId property. Path: {GetPath()}");
        if (AutoFocusFirst && !Engine.IsEditorHint()) CallDeferred(MethodName.FocusFirstForAllPlayers);
    }

    private void InitializePlayerState(Room.PlayerInfo playerInfo)
    {
        var state = new PlayerFocusState
        {
            PlayerIndex = playerInfo.PlayerIndex,
            FocusVisual = CreateFocusVisual(playerInfo)
        };

        if (playerInfo.OwnedByThisClient)
        {
            var input = state.Input = new GdfPlayerInput()
            {
                Name = $"Input {playerInfo.PlayerId}",
                UsesMouse = true,
                UsesKeyboard = true,
                PlayerId = playerInfo.PlayerId,
                InputAutoConfig = InputAutoConfig
            };
            input.SetMultiplayerAuthority(playerInfo.PeerId);
            _playerInputStates[input.PlayerId] = new PlayerInputState();
            _playerFocusStates[playerInfo.PlayerId] = state;
            AddChild(input);
        }
        else
        {
            _playerFocusStates[playerInfo.PlayerId] = state;
        }
    }

    private PlayerFocusVisual CreateFocusVisual(Room.PlayerInfo playerInfo)
    {
        PlayerFocusVisual focusVisual;
        if (FocusVisualTemplate != null)
            focusVisual = FocusVisualTemplate.Instantiate<PlayerFocusVisual>();
        else
            focusVisual = new PlayerFocusVisual()
                { SelfModulate = Colors.Transparent, MouseFilter = Control.MouseFilterEnum.Ignore };

        focusVisual.Name = $"Player Focus Visual {playerInfo.PlayerId}";
        focusVisual.MouseFilter = Control.MouseFilterEnum.Ignore;

        // TODO
        // focusVisual.InjectContext(new PlayerContext(playerInfo.PlayerId));
        return focusVisual;
    }

    private void SetPlayerFocusState(int playerId, PlayerFocusState state)
    {
        _playerFocusStates[playerId] = state;
    }

    public void FocusGroup(UserInterfaceGroup group)
    {
        _lastRequestedFocusedGroup = group;
        while (group != null && LastUsedInputType != 0 && (group.FocusableForInputTypes & LastUsedInputType) == 0)
            group = group.GetParentGroup();

        if (FocusedGroup == group) return;
        var prevGroup = FocusedGroup;
        FocusedGroup = group;
        EmitSignalFocusableGroupChanged(prevGroup, group);
    }

    public void FocusFirst(int playerId)
    {
        if (!IsInsideTree()) return;

        var firstFocusable = GetFirstValidFocusable();
        Focus(playerId, firstFocusable);
    }

    public void FocusFirstForAllPlayers()
    {
        var firstFocusable = GetFirstValidFocusable();

        switch (Operability)
        {
            case OperabilityEnum.AllPlayers:
            {
                foreach (var playerInfo in Room.Instance.GetAllPlayerInfo()) Focus(playerInfo.PlayerId, firstFocusable);

                break;
            }
            case OperabilityEnum.SpecificPlayer:
            {
                if (ExclusiveToPlayerId != -1)
                    Focus(ExclusiveToPlayerId, firstFocusable);
                break;
            }
            case OperabilityEnum.Playerless:
            case OperabilityEnum.PlayerlessAuthority:
            {
                Focus(PlayerlessId, firstFocusable);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public UserInterfaceComponent GetFirstValidFocusable()
    {
        if (!IsInsideTree()) return null;
        UserInterfaceComponent chosen = null;
        foreach (var focusable in _focusables)
            if (focusable.FocusableControl.IsVisibleInTree() && focusable.NavigableTo && focusable.NavigabilityConditionMet())
                if (chosen == null || focusable.AutoFocusPriority > chosen.AutoFocusPriority)
                    chosen = focusable;

        return chosen;
    }

    public UserInterfaceComponent GetFirstValidFocusableInGroup(UserInterfaceGroup group)
    {
        if (!IsInsideTree()) return null;
        UserInterfaceComponent chosen = null;
        foreach (var focusable in _focusables)
            if (focusable.NavigableTo && focusable.GetFocusableGroup() is { } focusableGroup &&
                focusableGroup.IsInsideFocusableGroup(group) && focusable.FocusableControl.IsVisibleInTree())
                if (chosen == null || focusable.AutoFocusPriority > chosen.AutoFocusPriority)
                    chosen = focusable;
        
        return chosen;
    }

    public void FocusForAllPlayers(Control control)
    {
        var focusable = control?.GetChildOfType<UserInterfaceComponent>();
        if (focusable?.FocusableControl == control) FocusForAllPlayers(focusable);
    }

    public void FocusForAllPlayers(UserInterfaceComponent focusable)
    {
        if (Operability is OperabilityEnum.Playerless or OperabilityEnum.PlayerlessAuthority)
            Focus(PlayerlessId, focusable);
        else
            foreach (var playerInfo in Room.Instance.GetAllPlayerInfo())
                Focus(playerInfo.PlayerId, focusable);
    }

    public void Focus(int playerId, Control control)
    {
        var focusable = control?.GetChildOfType<UserInterfaceComponent>();
        if (focusable?.FocusableControl == control) Focus(playerId, focusable);
    }

    public void UnfocusAll()
    {
        foreach ((int playerId, var state) in _playerFocusStates)
        {
            Focus(playerId, (UserInterfaceComponent)null);
            if (IsInstanceValid(state.FocusVisual))
                state.FocusVisual.QueueFree();
            state.Input?.QueueFree();
        }

        _playerFocusStates.Clear();
        _playerInputStates.Clear();
    }

    public void Unfocus(int playerId)
    {
        if (!_playerFocusStates.ContainsKey(playerId)) return;
        var state = _playerFocusStates[playerId];
        Focus(playerId, (UserInterfaceComponent)null);
        if (IsInstanceValid(state.FocusVisual))
            state.FocusVisual.QueueFree();
        state.Input?.QueueFree();
        _playerFocusStates.Remove(playerId);
        _playerInputStates.Remove(playerId);
    }

    public void Focus(int playerId, UserInterfaceComponent node)
    {
        if (!AcceptsPlayerId(playerId, out playerId, true, node == null)) return;

        if (Operability is not (OperabilityEnum.Playerless or OperabilityEnum.PlayerlessAuthority))
        {
            if (!Room.Instance.TryGetPlayerInfo(playerId, out var playerInfo)) return;
            if (!playerInfo.OwnedByThisClient) return;
        }

        if (!IsInsideTree() || (node != null && !node.IsInsideTree())) return;
        
        if (Operability is OperabilityEnum.PlayerlessAuthority && !IsMultiplayerAuthority()) return;

        if (ReplicateToPeers)
            this.CustomRpc(MethodName.FocusRpc, playerId, node);
        else
            FocusRpc(playerId, node);
    }

    [CustomRpc]
    private void FocusRpc(int playerId, UserInterfaceComponent node)
    {
        Room.PlayerInfo playerInfo;
        switch (Operability)
        {
            case OperabilityEnum.Playerless:
            case OperabilityEnum.PlayerlessAuthority:
            {
                playerId = PlayerlessId;
                playerInfo = new Room.PlayerInfo()
                {
                    PeerId = Operability is OperabilityEnum.PlayerlessAuthority ? GetMultiplayerAuthority() : Multiplayer.GetUniqueId(),
                    IndexInClient = 0,
                    PlayerId = PlayerlessId
                };
                break;
            }
            case OperabilityEnum.SpecificPlayer:
            case OperabilityEnum.AllPlayers:
            {
                if (!Room.Instance.TryGetPlayerInfo(playerId, out playerInfo)) return;
                break;
            }
            default:
                return;
        }

        var path = node != null ? GetPathTo(node) : null;

        if (!_playerFocusStates.ContainsKey(playerId))
        {
            InitializePlayerState(playerInfo);
        }
        var state = _playerFocusStates[playerId];

        var prevFocusable = state.FocusedNode;

        state.FocusedNodePath = path;
        state.FocusedNode = node;
        SetPlayerFocusState(playerId, state);
        UpdateAllVisualsAround(prevFocusable);
        UpdateAllVisualsAround(node);
        if (node == null) state.UpdateVisualAround(null, 0, 1);
        FirePlayerFocusChanged(playerId, prevFocusable, node);
    }

    private void FirePlayerFocusChanged(int playerId, UserInterfaceComponent from, UserInterfaceComponent to)
    {
        if (from == to) return;
        EmitSignal(SignalName.PlayerFocusChanged, playerId, from, to);
        from?.ExitFocus(playerId);
        to?.EnterFocus(playerId);
        // GD.Print($"Player {playerId} focus changed: {to?.GetPath()}");
    }

    public UserInterfaceComponent GetPlayerFocus(int playerId)
    {
        if (_playerFocusStates.TryGetValue(playerId, out var state)) return state.FocusedNode;

        return null;
    }

    public bool AnyPlayerHasFocus(UserInterfaceComponent focusable)
    {
        foreach ((int id, var state) in _playerFocusStates)
            if (state.FocusedNode == focusable)
                return true;

        return false;
    }

    private List<int> _tempFocusedPlayerIds;

    public List<int> GetAllFocusedPlayerIds()
    {
        _tempFocusedPlayerIds ??= new();
        _tempFocusedPlayerIds.Clear();
        foreach ((int id, var state) in _playerFocusStates)
        {
            _tempFocusedPlayerIds.Add(id);
        }
        return _tempFocusedPlayerIds;
    }

    private void UpdateAllVisualsAround(UserInterfaceComponent node)
    {
        if (node == null) return;
        // First, count number of players focused on this same node.
        var occurrenceCount = 0;
        foreach ((int playerId, var state) in _playerFocusStates)
        {
            if (state.FocusedNode != node) continue;
            if (state.FocusVisual == null) continue;
            occurrenceCount++;
        }

        bool overlapExists = occurrenceCount > 1;

        int occurrenceIndex = overlapExists ? 0 : -1;
        RepairFocusVisuals();
        var anyVisualsFailed = false;
        foreach ((int playerId, var state) in _playerFocusStates)
        {
            if (state.FocusedNode != node) continue;
            if (state.FocusVisual == null) continue;
            bool success = state.UpdateVisualAround(node.OutlinedControl, occurrenceIndex, occurrenceCount);
            if (!success) anyVisualsFailed = true;
            if (overlapExists)
                occurrenceIndex++;
        }

        if (anyVisualsFailed) CallDeferred(MethodName.RepairFocusVisuals, true);
    }

    private void RepairFocusVisuals(bool updateAroundElement = false)
    {
        foreach (int playerId in _playerFocusStates.Keys)
        {
            var state = _playerFocusStates[playerId];
            if (!state.IsFocusVisualValid())
            {
                state.FocusVisual = CreateFocusVisual(Room.Instance.GetPlayerInfo(playerId) ?? default);
                _playerFocusStates[playerId] = state;
                if (updateAroundElement) UpdateAllVisualsAround(state.FocusedNode);
            }
        }
    }

    public override void _Process(double delta)
    {
        if (!HasControl()) return;

        foreach ((int playerId, var state) in _playerFocusStates)
        {
            if (state.Input == null) continue;
            HandlePlayerInput(playerId, state, delta);
        }

        foreach (var focusable in _focusables)
        {
            if (!focusable.HasShortcut) continue;
            foreach ((int playerId, var state) in _playerFocusStates)
            {
                if (state.Input == null) continue;
                HandlePlayerShortcuts(playerId, state, focusable);
            }
        }
    }

    public bool HasControl()
    {
        return RequireFrameControl == null || RequireFrameControl.HasControl(RequireFrameControlPropertyId);
    }

    public GdfPlayerInput GetPlayerInput(int playerId)
    {
        if (_playerFocusStates.TryGetValue(playerId, out var state)) return state.Input;

        return null;
    }

    private void HandlePlayerInput(int playerId, PlayerFocusState state, double delta)
    {
        var inputState = _playerInputStates[playerId];
        var navigateVecRaw = state.Input.GetVec3(NavigateAction);

        inputState.TickCooldowns(delta);

        var navigateVec = new Vector2I(
            Mathf.Sign(navigateVecRaw.Dot(NavigateRightVec)),
            Mathf.Sign(navigateVecRaw.Dot(NavigateDownVec))
        );
        if (navigateVec != Vector2I.Zero && (inputState.Echoing || state.Input.ConsumeActionEvent(NavigateAction)))
        {
            UpdateInputType(NavigationInputType.ButtonsAndSticks);
            if (inputState.InputCooldown.X <= 0 && navigateVec.X != 0)
            {
                var side = navigateVec.X > 0 ? Side.Right : Side.Left;
                if (!(state.FocusedNode?.ConsumeNavigation(playerId, side) ?? false))
                    AttemptNavigation(playerId, ref state, navigateVec with { Y = 0 });

                inputState.InputCooldown.X = inputState.Echoing ? 0.1f : 0.5f;
            }

            if (inputState.InputCooldown.Y <= 0 && navigateVec.Y != 0)
            {
                var side = navigateVec.Y > 0 ? Side.Bottom : Side.Top;
                if (!(state.FocusedNode?.ConsumeNavigation(playerId, side) ?? false))
                    AttemptNavigation(playerId, ref state, navigateVec with { X = 0 });

                inputState.InputCooldown.Y = inputState.Echoing ? 0.1f : 0.5f;
            }
        }

        if (navigateVec.X == 0)
            inputState.InputCooldown.X = 0;
        if (navigateVec.Y == 0)
            inputState.InputCooldown.Y = 0;
        if (navigateVec == Vector2I.Zero)
            inputState.Echoing = false;

        if ((state.FocusedNode?.Submittable ?? false) && state.Input.ConsumeActionEvent(SubmitAction))
        {
            UpdateInputType(NavigationInputType.ButtonsAndSticks);
            state.FocusedNode?.Submit(playerId);
        }

        state.FocusedNode?.HandleSubActions(playerId, state.Input);

        _playerInputStates[playerId] = inputState;
    }

    private bool AttemptNavigation(int playerId, ref PlayerFocusState state, Vector2I navigateVec)
    {
        if (navigateVec == Vector2I.Zero) return false;

        var currentControl = state.FocusedNode?.FocusableControl;
        var nextControl = currentControl;
        if (currentControl != null)
        {
            if (navigateVec.X != 0 && nextControl != null)
                nextControl = nextControl.FindValidFocusNeighbor(navigateVec.X > 0 ? Side.Right : Side.Left);
            if (navigateVec.Y != 0 && nextControl != null)
                nextControl = nextControl.FindValidFocusNeighbor(navigateVec.Y > 0 ? Side.Bottom : Side.Top);
        }
        else
        {
            nextControl = GetFirstValidFocusable()?.FocusableControl;
        }

        var focusable = nextControl?.GetChildOfType<UserInterfaceComponent>();
        if (focusable != null && focusable.GetInterface() == this)
        {
            Focus(playerId, focusable);
            return true;
        }

        if (focusable != null)
            GD.PrintErr($"Attempted to navigate to a focusable of a different interface: {focusable.GetPath()}");

        return false;
    }

    private void HandlePlayerShortcuts(int playerId, PlayerFocusState state, UserInterfaceComponent focusable)
    {
        bool shortcutEnabled = focusable.IsShortcutEnabled(playerId);
        if (shortcutEnabled && state.Input.ConsumeActionEvent(focusable.ShortcutAction))
        {
            UpdateInputType(NavigationInputType.ButtonsAndSticks);
            bool alreadyFocused = state.FocusedNode == focusable;
            var shouldFocus = false;
            var shouldSubmit = false;
            switch (focusable.ShortcutMode)
            {
                case UserInterfaceComponent.ShortcutModeEnum.FocusOnly:
                    shouldFocus = true;
                    break;
                case UserInterfaceComponent.ShortcutModeEnum.OnceFocusTwiceSubmit:
                    shouldFocus = !alreadyFocused;
                    shouldSubmit = alreadyFocused;
                    break;
                case UserInterfaceComponent.ShortcutModeEnum.OnceFocusAndSubmit:
                    shouldFocus = true;
                    shouldSubmit = true;
                    break;
                case UserInterfaceComponent.ShortcutModeEnum.SubmitOnly:
                    shouldSubmit = true;
                    break;
            }

            if (shouldFocus) Focus(playerId, focusable);
            if (shouldSubmit) focusable.Submit(playerId);
        }
    }

    public void UpdateInputType(NavigationInputType type)
    {
        if (LastUsedInputType == type) return;
        // LastUsedInputType = type;
        GlobalLastUsedInputType = type;
        GlobalLastUsedInputTypeChanged?.Invoke();
        FocusGroup(_lastRequestedFocusedGroup);
    }
    
    private void OnInputDeviceChanged(int playerId)
    {
        if(_playerInputStates.ContainsKey(playerId)) EmitSignalInputTypeUpdated();
    }

    private void OnGlobalLastUsedInputTypeChanged()
    {
        EmitSignalInputTypeUpdated();
    }

    public void FocusableLoseFocus(UserInterfaceComponent userInterfaceComponent)
    {
        if (!IsInsideTree()) return;
        if (!this.GetParentOrNull<Control>().IsVisibleInTree()) return;
        UserInterfaceComponent nextFocusable = null;
        foreach ((int playerId, var state) in _playerFocusStates)
        {
            if (state.Input == null) continue; // no need to lose focus for non-local players
            if (state.FocusedNode != userInterfaceComponent) continue;

            nextFocusable ??= GetFallbackFocusableNode(userInterfaceComponent.FocusableControl);

            CallDeferred(MethodName.PlayerLoseFocus, playerId, nextFocusable, nextFocusable?.GetFocusableGroup());
        }
    }

    private void PlayerLoseFocus(int playerId, UserInterfaceComponent suggestedNextFocus, UserInterfaceGroup suggestedGroup)
    {
        if (suggestedNextFocus != null &&
            (!IsInstanceValid(suggestedNextFocus) || suggestedNextFocus.IsQueuedForDeletion()))
            suggestedNextFocus = null;

        if (suggestedNextFocus != null && !suggestedNextFocus.IsInsideTree())
            suggestedNextFocus = null;

        suggestedNextFocus ??= GetFirstValidFocusableInGroup(suggestedGroup);
        Focus(playerId, suggestedNextFocus);
    }

    private UserInterfaceComponent GetFallbackFocusableNode(Control from)
    {
        Control nextControl = null;

        if (from.IsVisibleInTree())
        {
            nextControl ??= from.FindPrevValidFocus();
            nextControl ??= from.FindNextValidFocus();
        }

        var focusable = nextControl?.GetChildOfType<UserInterfaceComponent>();
        if (focusable != null && focusable.GetInterface() == this)
            return focusable;

        return GetFirstValidFocusable();
    }

    public void AddFocusable(UserInterfaceComponent focusable)
    {
        if (!_focusables.Contains(focusable)) TreeOrderUtil.InsertInTreeOrder(_focusables, focusable);
    }

    public void RemoveFocusable(UserInterfaceComponent focusable)
    {
        _focusables.Remove(focusable);
    }

    public void AddFocusableGroup(UserInterfaceGroup group)
    {
        // no need for this yet
    }

    public void RemoveFocusableGroup(UserInterfaceGroup group)
    {
        // no need for this yet
    }

    public bool AcceptsPlayerId(int playerId, bool mustBeLocal = false)
    {
        return AcceptsPlayerId(playerId, out _, mustBeLocal);
    }

    public bool AcceptsPlayerId(int playerId, out int transformedId, bool mustBeLocal = false,
        bool bypassExclusiveCheck = false)
    {
        transformedId = playerId;
        switch (Operability)
        {
            case OperabilityEnum.Playerless:
            case OperabilityEnum.PlayerlessAuthority:
            {
                transformedId = PlayerlessId;
                return true;
            }
            case OperabilityEnum.SpecificPlayer:
            {
                if (!bypassExclusiveCheck && ExclusiveToPlayerId != -1 && playerId != ExclusiveToPlayerId) return false;
                if (!Room.Instance.TryGetPlayerInfo(playerId, out var playerInfo)) return false;
                if (mustBeLocal && !playerInfo.OwnedByThisClient) return false;
                return true;
            }
            case OperabilityEnum.AllPlayers:
            {
                if (!Room.Instance.TryGetPlayerInfo(playerId, out var playerInfo)) return false;
                if (mustBeLocal && !playerInfo.OwnedByThisClient) return false;
                return true;
            }
        }

        return true;
    }

    private static string FirstNonEmpty(params string[] strings)
    {
        foreach (string str in strings)
            if (!string.IsNullOrEmpty(str))
                return str;

        return null;
    }

    public void SetExclusiveToPlayerId(int playerId)
    {
        ExclusiveToPlayerId = playerId;
    }

    public override void _EnterTree()
    {
        if (Engine.IsEditorHint()) return;
        this.Resync();
        RequestReady();
        AddToGroup(Group);
        
        if (GdfInputSystem.Instance != null) GdfInputSystem.Instance.PlayerChangedInputDevice += OnInputDeviceChanged;
        GlobalLastUsedInputTypeChanged += OnGlobalLastUsedInputTypeChanged;
    }

    public override void _ExitTree()
    {
        if (Engine.IsEditorHint()) return;
        
        if (GdfInputSystem.Instance != null) GdfInputSystem.Instance.PlayerChangedInputDevice -= OnInputDeviceChanged;
        GlobalLastUsedInputTypeChanged -= OnGlobalLastUsedInputTypeChanged;
    }

    // public void GetContextActions(List<ContextAction> output)
    // {
    //     foreach ((int playerId, var state) in _playerFocusStates)
    //     {
    //         if (state.Input == null) continue;
    //         if (NavigateAction != null && ShowNavigateContextAction)
    //             output.Add(new ContextAction(NavigateAction, playerId, OverrideNavigateText));
    //         if (SubmitAction != null)
    //         {
    //             if (state.FocusedNode is { Submittable: true, ShowSubmitContextAction: true })
    //                 output.Add(new ContextAction(SubmitAction, playerId,
    //                     FirstNonEmpty(state.FocusedNode.OverrideSubmitText, OverrideSubmitText)));
    //             if (state.FocusedNode is { SubActions: not null, ShowSubActionContextAction: true })
    //                 foreach (var subAction in state.FocusedNode.SubActions.Keys)
    //                     output.Add(new ContextAction(subAction, playerId,
    //                         state.FocusedNode.OverrideSubActionTexts?.GetValueOrDefault(subAction)));
    //         }
    //     }
    //
    //     foreach (var focusable in _focusables)
    //     {
    //         if (!focusable.HasShortcut || !focusable.ShowShortcutContextAction) continue;
    //         foreach ((int playerId, var state) in _playerFocusStates)
    //         {
    //             if (state.Input == null) continue;
    //             if (!focusable.IsShortcutEnabled(playerId)) continue;
    //             output.Add(new ContextAction(focusable.ShortcutAction, playerId, focusable.OverrideShortcutText));
    //         }
    //     }
    // }

    [CustomRpc]
    public void Resync(int peerId)
    {
    }

    private struct PlayerFocusState
    {
        // Synced
        public NodePath FocusedNodePath;

        // Local
        public UserInterfaceComponent FocusedNode;
        public PlayerFocusVisual FocusVisual;
        public GdfPlayerInput Input;
        public int PlayerIndex;

        public bool IsFocusVisualValid()
        {
            return IsInstanceValid(FocusVisual) && !FocusVisual.IsQueuedForDeletion();
        }

        public bool UpdateVisualAround(Control control, int occurrenceIndex, int totalOccurrences)
        {
            if (!IsFocusVisualValid()) return false;

            if (FocusVisual.GetParent() is { } oldParent)
            {
                oldParent.RemoveChild(FocusVisual);
                if (FocusVisual.GetParent() != null)
                {
                    GD.PrintErr("^ ^ ^ This error was handled ^ ^ ^");
                    // Failed to remove - assume oldParent is getting freed and there's no way to rescue the visual.
                    FocusVisual.QueueFree();
                    return control == null;
                }
            }

            if (control == null) return true;
            var newParent = control;
            newParent.AddChild(FocusVisual, @internal: InternalMode.Front);

            FocusVisual.Position = Vector2.Zero;
            FocusVisual.SetSize(control.Size);
            FocusVisual.Update(occurrenceIndex, totalOccurrences);
            return true;
        }

        public Variant Serialize(UserInterface owner)
        {
            return FocusedNodePath;
        }

        public void Deserialize(Variant raw, UserInterface owner)
        {
            var newPath = raw.AsNodePath();
            if (FocusedNodePath.IsNullOrEmpty())
            {
                FocusedNodePath = null;
                FocusedNode = null;
            }
            else
            {
                FocusedNodePath = newPath;
                FocusedNode = owner.GetNodeOrNull<UserInterfaceComponent>(newPath);
            }
        }
    }

    private struct PlayerInputState
    {
        public Vector2 InputCooldown;
        public bool Echoing;

        public void TickCooldowns(double delta)
        {
            if (InputCooldown.X > 0)
            {
                InputCooldown.X -= (float)delta;
                if (InputCooldown.X < 0)
                {
                    InputCooldown.X = 0;
                    Echoing = true;
                }
            }

            if (InputCooldown.Y > 0)
            {
                InputCooldown.Y -= (float)delta;
                if (InputCooldown.Y < 0)
                {
                    InputCooldown.Y = 0;
                    Echoing = true;
                }
            }
        }
    }

    [Flags]
    public enum NavigationInputType
    {
        ButtonsAndSticks = 1 << 0,
        Mouse = 1 << 1,
        Touch = 1 << 2
    }

    public enum OperabilityEnum
    {
        AllPlayers,
        SpecificPlayer,
        Playerless,
        PlayerlessAuthority
    }
}