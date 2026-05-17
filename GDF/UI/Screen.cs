using System;
using GDF.Logical;
using GDF.Multiplayer;
using GDF.Networking;
using GDF.PropertyStacks;
using GDF.PropertyStacks.Definitions.Specialized;
using GDF.Util;
using Godot;

namespace GDF.UI;

[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/screen.png")]
public partial class Screen : Control
{
    [Signal]
    public delegate void ScreenShownEventHandler();

    [Signal]
    public delegate void ScreenFadingOutEventHandler();

    [Signal]
    public delegate void ScreenHiddenEventHandler();

    [Signal]
    public delegate void ScreenShadowingStartedEventHandler();

    [Signal]
    public delegate void ScreenShadowingEndedEventHandler();

    [Export] public int Order = 0;
    [Export] public ScreenModeEnum Mode = ScreenModeEnum.Overlay;
    [Export] public bool AutomaticLayering = true;
    [Export] public UserInterface UserInterface;

    [ExportGroup("Layering")]
    [Export] public bool OrderAsRelative = false;
    
    [ExportGroup("Shadowing")]
    [Export] public bool ShadowLowerOrderInterfaces = false;
    [Export] public bool HideOnShadowed = true;

    /// <summary>
    /// Controls which peers are allowed to call methods (such as <see cref="ShowScreen"/>/<see cref="HideScreen"/>) on this Screen.
    /// Does not affect Force___ methods.
    /// </summary>
    [ExportGroup("Networking")]
    [Export] public AuthorityMode AuthorityMode = AuthorityMode.AnyPeer;

    /// <summary>
    /// If true, calls to methods (such as <see cref="ShowScreen"/>/<see cref="HideScreen"/>) on this Screen will be replicated to peers.
    /// Does not affect Force___ methods.
    /// </summary>
    [Export] public bool ReplicateToPeers;

    public int ExclusiveToPlayerId
    {
        get => _exclusiveToPlayerId;
        set
        {
            _exclusiveToPlayerId = value;
            if (UserInterface != null) UserInterface.ExclusiveToPlayerId = value;
        }
    }

    public NodePath OriginalNodePath;
    public PropertyFrame ControlFrame;

    private ScreenPlaceholder _placeholder;
    private bool _customInstantiated = false;
    public bool Showing = false;
    public bool ShowingInTree { get; private set; } = false;
    public bool FadingOut { get; private set; } = false;
    public Node SpawnedByNode = null;
    public Node OriginalOwner;
    public bool Shadowed = false;
    private int _exclusiveToPlayerId = -1;

    public override void _Ready()
    {
        OriginalOwner ??= Owner;
        if (AutomaticLayering)
        {
            TopLevel = true;
        }
        else if (Visible)
        {
            ShowScreen();
        }
        if (!_customInstantiated && AutomaticLayering)
            GD.PushWarning(
                $"Screen {Name} was added to the scene tree without being properly instantiated via GdfInstantiate()! This is currently not supported for Screens with automatic layering.\nIf you intended this Screen to not use automatic layering, set the property to false.");
        else if (AutomaticLayering && _customInstantiated && _placeholder == null)
        {
            GD.PushWarning(
                $"Screen {Name} was added to the scene tree without ToPlaceholder being called or properly handled. When using GdfInstantiate to instantiate a Screen, please call ToPlaceholder and add the returned node to the tree instead of the Screen directly.");
        }
    }

    public void ShowScreen()
    {
        if (!AuthorityMode.CanExecuteNoTree(this)) return;

        NetworkingCall(MethodName.ForceShowScreen);
    }

    [GdfRpc]
    public void ForceRefreshShownState()
    {
        if(Showing) ForceShowScreen();
        else ForceHideScreen();
    }

    [GdfRpc]
    public void ForceShowScreen()
    {
        if (!ShowingInTree && (_placeholder?.PrepareScreenToEnterTree() ?? true))
        {
            // Prepare controls
            var affectedStack = Mode switch
            {
                ScreenModeEnum.Overlay => GlobalPropertyStack.Instance,
                ScreenModeEnum.Modal => GlobalPropertyStack.Instance,
                ScreenModeEnum.NonModal => PerPlayerPropertyStacks.GetForPlayer(ExclusiveToPlayerId),
                ScreenModeEnum.ParallelLane => PerPlayerPropertyStacks.GetForPlayer(ExclusiveToPlayerId),
                ScreenModeEnum.PassthroughNonModal => PerPlayerPropertyStacks.GetForPlayer(ExclusiveToPlayerId),
                _ => throw new ArgumentOutOfRangeException()
            };
            var inputGroupMode = Mode switch
            {
                ScreenModeEnum.Overlay => InputGroupMode.PassThrough,
                ScreenModeEnum.Modal => InputGroupMode.Capture,
                ScreenModeEnum.NonModal => InputGroupMode.Capture,
                ScreenModeEnum.ParallelLane => InputGroupMode.PassThrough,
                ScreenModeEnum.PassthroughNonModal => InputGroupMode.PassThrough,
                _ => throw new ArgumentOutOfRangeException()
            };
            ControlFrame ??= affectedStack?.NewFrame($"Screen: {Name}", GetEffectiveOrder())
                .BindToNode(this);

            foreach (string id in InputGroups.GetAll())
                ControlFrame?.Set(id, inputGroupMode);

            if (UserInterface != null)
            {
                UserInterface.ExclusiveToPlayerId = ExclusiveToPlayerId;
                if (Mode == ScreenModeEnum.ParallelLane && UserInterface.RequireFrameControl == null)
                {
                    var modalParent = FindAncestorScreen(this);
                    if (modalParent?.Mode == ScreenModeEnum.Modal)
                        UserInterface.RequireFrameControl = modalParent?.ControlFrame;
                    if (UserInterface.RequireFrameControl == null)
                        GD.PushWarning(
                            $"Parallel Lane Screen {Name} does not have the user interface's {nameof(UserInterface.RequireFrameControl)} set, and no modal parent screen.\nBe sure to spawn it with a 'Make Screen Parallel Lane Of' property set in the ObjectSpawner!");
                }

                UserInterface.RequireFrameControl ??= ControlFrame;
            }

            Visible = true;
            Showing = true;
            ShowingInTree = true;
            FadingOut = false;
            if (AutomaticLayering && !IsInsideTree())
                ScreenStack.Instance.Push(this);
            EmitSignalScreenShown();
        }
        else
        {
            Showing = true;
        }
    }

    public int GetEffectiveOrder()
    {
        if (OrderAsRelative)
        {
            return FindAncestorScreenOrder(this.GetParent(), 0) + Order;
        }
        return Order;
    }

    public void HideScreen()
    {
        if (!AuthorityMode.CanExecuteNoTree(this)) return;

        NetworkingCall(MethodName.ForceHideScreen);
    }

    [GdfRpc]
    public void ForceHideScreen()
    {
        if (IsInsideTree() && (ShowingInTree || Visible || FadingOut))
        {
            if (ControlFrame != null && UserInterface?.RequireFrameControl == ControlFrame && UserInterface != null)
                UserInterface.RequireFrameControl = null;
            ControlFrame = ControlFrame?.Remove();

            if (AutomaticLayering)
                ScreenStack.Instance.RemoveChild(this);
            Visible = false;
            ShowingInTree = false;
            Showing = false;
            FadingOut = false;
            EmitSignalScreenHidden();
        }
        else
        {
            Showing = false;
        }
    }

    public void FadeOutScreen()
    {
        if (!AuthorityMode.CanExecuteNoTree(this)) return;

        NetworkingCall(MethodName.ForceFadeOutScreen);
    }

    [GdfRpc]
    public void ForceFadeOutScreen()
    {
        if (IsInsideTree() && ShowingInTree)
        {
            if (ControlFrame != null && Mode is ScreenModeEnum.Modal or ScreenModeEnum.NonModal)
            {
                foreach (string id in InputGroups.GetAll())
                    ControlFrame.Set(id, InputGroupMode.Disable);
            }

            ShowingInTree = false;
            Showing = false;
            FadingOut = true;
            EmitSignalScreenFadingOut();
            ScreenStack.Instance.UpdateLayeredVisibility();
        }
        else
        {
            Showing = false;
        }
    }

    public void HideAndFreeScreen()
    {
        if (!AuthorityMode.CanExecuteNoTree(this)) return;

        NetworkingCall(MethodName.ForceHideAndFreeScreen);
    }

    [GdfRpc]
    public void ForceHideAndFreeScreen()
    {
        ForceHideScreen();
        QueueFree();
        _placeholder?.QueueFree();
    }

    private void NetworkingCall(StringName methodName, params Variant[] args)
    {
        if (ReplicateToPeers)
        {
            if (_placeholder != null)
            {
                // call it on the placeholder
                if (_placeholder.IsInsideTree())
                    _placeholder.GdfRpc(ScreenPlaceholder.MethodName.CallScreenMethod, methodName, new Godot.Collections.Array(args));
                else
                    GD.PrintErr(
                        $"Tried to call method {methodName} on Screen {Name}, but the placeholder is not inside the tree");
            }
            else if (IsInsideTree())
            {
                this.GdfRpc(methodName, args);
            }
            else
            {
                GD.PrintErr($"Tried to call method {methodName} on Screen {Name}, but it is not inside the tree");
            }
        }
        else
        {
            Call(methodName, args);
        }
    }

    public override void _Notification(int what)
    {
        if (what == GdfConstants.NotificationDeepSceneInstantiated)
        {
            _customInstantiated = true;
            this.ConnectToSceneInstantiatedSignal(new Callable(this, MethodName.SceneReady), ConnectFlags.OneShot);
            OriginalOwner = Owner;
        }

        if (what == NotificationExitTree)
            if (!AutomaticLayering)
                HideScreen();
        if (what == NotificationPredelete)
            if (IsInstanceValid(_placeholder))
                _placeholder.QueueFree();
    }

    private void SceneReady()
    {
        Showing = Visible;
        ToPlaceholder(true);
    }

    public Node ToPlaceholder()
    {
        return ToPlaceholder(false);
    }

    private Node ToPlaceholder(bool replaceInParent)
    {
        if (!AutomaticLayering) return this;
        if (_placeholder != null) return _placeholder;

        var parent = GetParent();
        if (parent == null) replaceInParent = false;
        
        int index = GetIndex();
        var prevName = Name;
        var prevOwner = Owner;
        // Remove this Screen from the scene
        if (replaceInParent)
            parent.RemoveChild(this);

        // Replace it with a new UserInterfacePlaceholder which has a reference to this
        var placeholder = new ScreenPlaceholder(this)
            { Name = prevName };
        _placeholder = placeholder;
        if (replaceInParent)
        {
            parent.AddChild(placeholder);
            parent.MoveChild(placeholder, index);
        }

        placeholder.Owner = prevOwner;
        placeholder.SetMultiplayerAuthority(GetMultiplayerAuthority());

        return placeholder;
    }

    public void MakeParallelLaneOf(Screen modalParent)
    {
        if (UserInterface != null)
            UserInterface.RequireFrameControl = modalParent.ControlFrame;
    }

    public Node GetOriginalOwner()
    {
        return OriginalOwner;
    }

    public static Screen FindAncestorScreen(Node source)
    {
        source = source?.GetParent();
        while (source != null)
            if (source is Screen screen) return screen;
            else source = source.GetParent();

        return null;
    }

    public static int FindAncestorScreenOrder(Node source, int fallback)
    {
        return FindAncestorScreen(source)?.GetEffectiveOrder() ?? fallback;
    }

    public enum ScreenModeEnum
    {
        Overlay,
        Modal,
        NonModal,
        ParallelLane,
        PassthroughNonModal
    }
}