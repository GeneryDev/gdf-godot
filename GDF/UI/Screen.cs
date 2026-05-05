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
    public delegate void UIShownEventHandler();

    [Signal]
    public delegate void UIFadingOutEventHandler();

    [Signal]
    public delegate void UIHiddenEventHandler();

    [Signal]
    public delegate void UIShadowingStartedEventHandler();

    [Signal]
    public delegate void UIShadowingEndedEventHandler();

    [Export] public int Order = 0;
    [Export] public UIModeEnum Mode = UIModeEnum.Overlay;
    [Export] public bool AutomaticLayering = true;
    [Export] public UserInterface UserInterface;

    [ExportGroup("Layering")]
    [Export] public bool InheritParentControlRect = true;

    [Export] public bool OrderAsRelative = false;
    
    [ExportGroup("Shadowing")]
    [Export] public bool ShadowLowerOrderInterfaces = false;
    [Export] public bool HideOnShadowed = true;

    /// <summary>
    /// Controls which peers are allowed to call methods (such as <see cref="ShowUI"/>/<see cref="HideUI"/>) on this UI.
    /// Does not affect Force___ methods.
    /// </summary>
    [ExportGroup("Networking")]
    [Export] public AuthorityMode AuthorityMode = AuthorityMode.AnyPeer;

    /// <summary>
    /// If true, calls to methods (such as <see cref="ShowUI"/>/<see cref="HideUI"/>) on this UI will be replicated to peers.
    /// Does not affect Force___ methods.
    /// </summary>
    [Export] public bool ReplicateToPeers;

    public int ExclusiveToPlayerId = -1;

    public NodePath OriginalNodePath;
    public PropertyFrame ControlFrame;

    private ScreenPlaceholder _placeholder;
    private bool _customInstantiated = false;
    public bool Showing = false;
    public bool FadingOut { get; private set; } = false;
    public Node SpawnedByNode = null;
    public Node OriginalOwner;
    public bool Shadowed = false;

    public override void _Ready()
    {
        OriginalOwner ??= Owner;
        if (AutomaticLayering) TopLevel = true;

        if (Visible && !AutomaticLayering) ShowUI();
        if (!_customInstantiated && AutomaticLayering)
            GD.PushWarning(
                $"User interface {Name} was added to the scene tree without being properly instantiated via EGInstantiate()! This is currently not supported for UIs with automatic layering.\nIf you intended this UI to not use automatic layering, set the property to false.");
    }

    public void ShowUI()
    {
        if (!AuthorityMode.CanExecuteNoTree(this)) return;

        NetworkingCall(MethodName.ForceShowUI);
    }

    [CustomRpc]
    public void ForceShowUI()
    {
        if (!Showing && (_placeholder?.PrepareRootToEnterTree() ?? true))
        {
            // Prepare controls
            var affectedStack = Mode switch
            {
                UIModeEnum.Overlay => GlobalPropertyStack.Instance,
                UIModeEnum.Modal => GlobalPropertyStack.Instance,
                UIModeEnum.NonModal => PerPlayerPropertyStacks.GetForPlayer(ExclusiveToPlayerId),
                UIModeEnum.ParallelLane => PerPlayerPropertyStacks.GetForPlayer(ExclusiveToPlayerId),
                UIModeEnum.PassthroughNonModal => PerPlayerPropertyStacks.GetForPlayer(ExclusiveToPlayerId),
                _ => throw new ArgumentOutOfRangeException()
            };
            var inputGroupMode = Mode switch
            {
                UIModeEnum.Overlay => InputGroupMode.PassThrough,
                UIModeEnum.Modal => InputGroupMode.Capture,
                UIModeEnum.NonModal => InputGroupMode.Capture,
                UIModeEnum.ParallelLane => InputGroupMode.PassThrough,
                UIModeEnum.PassthroughNonModal => InputGroupMode.PassThrough,
                _ => throw new ArgumentOutOfRangeException()
            };
            ControlFrame ??= affectedStack?.NewFrame($"Screen: {Name}", GetEffectiveOrder())
                .BindToNode(this);

            foreach (string id in InputGroups.GetAll())
                ControlFrame?.Set(id, inputGroupMode);

            if (UserInterface != null)
            {
                UserInterface.ExclusiveToPlayerId = ExclusiveToPlayerId;
                if (Mode == UIModeEnum.ParallelLane && UserInterface.RequireFrameControl == null)
                {
                    var modalParent = FindAncestorUI(this);
                    if (modalParent?.Mode == UIModeEnum.Modal)
                        UserInterface.RequireFrameControl = modalParent?.ControlFrame;
                    if (UserInterface.RequireFrameControl == null)
                        GD.PushWarning(
                            $"Parallel Lane Screen {Name} does not have the user interface's {nameof(UserInterface.RequireFrameControl)} set, and no modal parent screen.\nBe sure to spawn it with a 'Make Screen Parallel Lane Of' property set in the ObjectSpawner!");
                }

                UserInterface.RequireFrameControl ??= ControlFrame;
            }

            Visible = true;
            Showing = true;
            FadingOut = false;
            if (AutomaticLayering && !IsInsideTree())
                ScreenStack.Instance.Push(this);
            EmitSignalUIShown();
        }
    }

    public int GetEffectiveOrder()
    {
        if (OrderAsRelative)
        {
            return FindAncestorUIOrder(this.GetParent(), 0) + Order;
        }
        return Order;
    }

    public void HideUI()
    {
        if (!AuthorityMode.CanExecuteNoTree(this)) return;

        NetworkingCall(MethodName.ForceHideUI);
    }

    [CustomRpc]
    public void ForceHideUI()
    {
        if (IsInsideTree() && (Showing || Visible || FadingOut))
        {
            if (ControlFrame != null && UserInterface?.RequireFrameControl == ControlFrame && UserInterface != null)
                UserInterface.RequireFrameControl = null;
            ControlFrame = ControlFrame?.Remove();

            if (AutomaticLayering)
                ScreenStack.Instance.RemoveChild(this);
            Visible = false;
            Showing = false;
            FadingOut = false;
            EmitSignalUIHidden();
        }
    }

    public void FadeOutUI()
    {
        if (!AuthorityMode.CanExecuteNoTree(this)) return;

        NetworkingCall(MethodName.ForceFadeOutUI);
    }

    [CustomRpc]
    public void ForceFadeOutUI()
    {
        if (IsInsideTree() && Showing)
        {
            if (ControlFrame != null && Mode is UIModeEnum.Modal or UIModeEnum.NonModal)
            {
                foreach (string id in InputGroups.GetAll())
                    ControlFrame.Set(id, InputGroupMode.Disable);
            }

            Showing = false;
            FadingOut = true;
            EmitSignalUIFadingOut();
            ScreenStack.Instance.UpdateLayeredVisibility();
        }
    }

    public void HideAndFreeUI()
    {
        if (!AuthorityMode.CanExecuteNoTree(this)) return;

        NetworkingCall(MethodName.ForceHideAndFreeUI);
    }

    [CustomRpc]
    public void ForceHideAndFreeUI()
    {
        ForceHideUI();
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
                    _placeholder.CustomRpc(ScreenPlaceholder.MethodName.CallUIMethod, methodName, new Godot.Collections.Array(args));
                else
                    GD.PrintErr(
                        $"Tried to call method {methodName} on UI root {Name}, but the placeholder is not inside the tree");
            }
            else if (IsInsideTree())
            {
                this.CustomRpc(methodName, args);
            }
            else
            {
                GD.PrintErr($"Tried to call method {methodName} on UI root {Name}, but it is not inside the tree");
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
                HideUI();
        if (what == NotificationPredelete)
            if (IsInstanceValid(_placeholder))
                _placeholder.QueueFree();
    }

    private void SceneReady()
    {
        var prevParent = GetParent();
        if (prevParent != null)
            ToPlaceholder(prevParent, true);
    }

    public Node ToPlaceholder(Node parent, bool deriveNameFromPath)
    {
        if (!AutomaticLayering) return this;
        if (parent == null) return this;
        if (_placeholder != null)
        {
            GD.PrintErr($"This {nameof(Screen)} (named '{Name}') already has a placeholder!");
            return _placeholder;
        }

        int index = GetIndex();
        var prevName = Name;
        var prevOwner = Owner;
        bool replaceInParent = GetParent() == parent;
        // Remove this UI from the scene
        if (replaceInParent)
            parent.RemoveChild(this);

        // Replace it with a new UserInterfacePlaceholder which has a reference to this
        var placeholder = new ScreenPlaceholder(this)
            { Name = prevName, DeriveNameFromPath = deriveNameFromPath };
        _placeholder = placeholder;
        if (replaceInParent)
        {
            parent.AddChild(placeholder);
            parent.MoveChild(placeholder, index);
        }

        placeholder.Owner = prevOwner;
        placeholder.SetMultiplayerAuthority(GetMultiplayerAuthority());

        // Inherit parent control position if spawned inside a control
        if (parent is Control parentControl && InheritParentControlRect)
        {
            var parentRect = parentControl.GetGlobalRect();
            Position = parentRect.Position;
            Size = parentRect.Size;
        }
        return placeholder;
    }

    public int GetExclusiveToPlayerId()
    {
        return ExclusiveToPlayerId;
    }

    public void SetExclusiveToPlayerId(int value)
    {
        if (!AuthorityMode.CanExecuteNoTree(this)) return;

        NetworkingCall(MethodName.ForceSetExclusiveToPlayerId, value);
    }

    [CustomRpc]
    public void ForceSetExclusiveToPlayerId(int value)
    {
        ExclusiveToPlayerId = value;
        if (UserInterface != null) UserInterface.ExclusiveToPlayerId = ExclusiveToPlayerId;
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

    public static Screen FindAncestorUI(Node source)
    {
        source = source?.GetParent();
        while (source != null)
            if (source is Screen ui) return ui;
            else source = source.GetParent();

        return null;
    }

    public static int FindAncestorUIOrder(Node source, int fallback)
    {
        return FindAncestorUI(source)?.GetEffectiveOrder() ?? fallback;
    }

    public enum UIModeEnum
    {
        Overlay,
        Modal,
        NonModal,
        ParallelLane,
        PassthroughNonModal
    }
}