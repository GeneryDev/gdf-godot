using System;
using GDF.Attributes;
using GDF.Attributes.Impl;
using GDF.Input;
using GDF.Logical;
using GDF.Multiplayer;
using GDF.Networking;
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
    [Export(PropertyHint.Enum,"Don't Change:-1,Visible:0,Hidden:1,Captured:2,Confined:3,Confined Hidden:4")] public int MouseMode = (int)Godot.Input.MouseModeEnum.Visible;

    [ExportGroup("Layering")]
    [Export] public bool FollowParentControlRect = false;
    [Export] public bool OrderRelativeToAncestor = false;
    
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
    public BulkModifier ControlFrame;

    private ScreenPlaceholder _placeholder;
    private bool _customInstantiated = false;
    public bool Showing = false;
    public bool ShowingInTree { get; private set; } = false;
    public bool FadingOut { get; private set; } = false;
    public Node SpawnedByNode = null;
    public Node OriginalOwner;
    public bool Shadowed = false;
    
    private int _exclusiveToPlayerId = -1;
    private BulkModifier _globalFrame;
    private CanvasLayer _layer;

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
            var affectedGroups = GetAffectedInputGroups();
            var inputGroupMode = Mode switch
            {
                ScreenModeEnum.Overlay => InputGroupMode.PassThrough,
                ScreenModeEnum.Modal => InputGroupMode.Capture,
                ScreenModeEnum.NonModal => InputGroupMode.Capture,
                ScreenModeEnum.ParallelLane => InputGroupMode.PassThrough,
                ScreenModeEnum.PassthroughNonModal => InputGroupMode.PassThrough,
                _ => throw new ArgumentOutOfRangeException()
            };
            ControlFrame ??= new BulkModifier($"Screen: {Name}", GetEffectiveOrder())
                .BindToNode(this);

            foreach (var stack in affectedGroups.GetAll().Values)
                ControlFrame?.Modify(stack, inputGroupMode);

            if (MouseMode != -1)
            {
                var cursorFrame = affectedGroups == PlayerInputGroups.Global
                    ? ControlFrame
                    : _globalFrame = new BulkModifier($"Screen Cursor: {Name}")
                        .BindToNode(this);
                cursorFrame?.Modify(EngineAttributes.Instance.MouseMode, (Godot.Input.MouseModeEnum)MouseMode);
            }

            if (UserInterface != null)
            {
                UserInterface.ExclusiveToPlayerId = ExclusiveToPlayerId;
                if (Mode == ScreenModeEnum.ParallelLane && UserInterface.ControlRequirement == null)
                {
                    var modalParent = FindAncestorScreen(this);
                    if (modalParent?.Mode == ScreenModeEnum.Modal)
                        UserInterface.ControlRequirement = modalParent.HasControl;
                    if (UserInterface.ControlRequirement == null)
                        GD.PushWarning(
                            $"Parallel Lane Screen {Name} does not have the user interface's {nameof(UserInterface.ControlRequirement)} set, and no modal parent screen.\nBe sure to spawn it with a 'Make Screen Parallel Lane Of' property set in the ObjectSpawner!");
                }

                UserInterface.ControlRequirement ??= this.HasControl;
            }

            _globalFrame = new BulkModifier($"Screen: {Name} Cursor");

            Visible = true;
            Showing = true;
            ShowingInTree = true;
            FadingOut = false;
            if (AutomaticLayering && !IsInsideTree())
            {
                ScreenStack.Instance.Push(this);
                UpdateRect();
            }
            EmitSignalScreenShown();
        }
        else
        {
            Showing = true;
        }
    }

    private InputGroups GetAffectedInputGroups()
    {
        return Mode switch
        {
            ScreenModeEnum.Overlay => PlayerInputGroups.Global,
            ScreenModeEnum.Modal => PlayerInputGroups.Global,
            ScreenModeEnum.NonModal => PlayerInputGroups.GetForPlayer(ExclusiveToPlayerId),
            ScreenModeEnum.ParallelLane => PlayerInputGroups.GetForPlayer(ExclusiveToPlayerId),
            ScreenModeEnum.PassthroughNonModal => PlayerInputGroups.GetForPlayer(ExclusiveToPlayerId),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public int GetEffectiveOrder()
    {
        if (OrderRelativeToAncestor)
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
            if (ControlFrame != null && UserInterface?.ControlRequirement == this.HasControl && UserInterface != null)
                UserInterface.ControlRequirement = null;
            RemoveFrames();

            if (AutomaticLayering)
                ScreenStack.Instance.Remove(this);
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

    private void RemoveFrames()
    {
        ControlFrame = ControlFrame?.Discard();
        _globalFrame = _globalFrame?.Discard();
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
                foreach (var stack in GetAffectedInputGroups().GetAll().Values)
                    ControlFrame?.Modify(stack, InputGroupMode.Disable);
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

    private void UpdateRect()
    {
        if (FollowParentControlRect && _placeholder?.GetParentOrNull<Control>() is {} parentControl && parentControl.IsInsideTree())
        {
            var globalRect = parentControl.GetGlobalRect();
            this.Position = globalRect.Position;
            this.Size = globalRect.Size;
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        UpdateRect();
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
        {
            RemoveFrames();
            
            if (!AutomaticLayering)
                HideScreen();
        }

        if (what == NotificationPredelete)
        {
            if(IsInstanceValid(_layer))
                _layer.QueueFree();
            if (IsInstanceValid(_placeholder))
                _placeholder.QueueFree();
        }
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
            UserInterface.ControlRequirement = modalParent.HasControl;
    }

    public bool HasControl()
    {
        return ControlFrame == null || ControlFrame.HasControl(GetAffectedInputGroups().Get(GdfConstants.UserInterfaceRequiredInputGroup));
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

    public CanvasLayer GetOrCreateLayer()
    {
        if (_layer == null)
        {
            _layer = new CanvasLayer();
            _layer.AddChild(this);
        }
        _layer.Name = this.Name;
        _layer.Layer = GetEffectiveOrder();
        return _layer;
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