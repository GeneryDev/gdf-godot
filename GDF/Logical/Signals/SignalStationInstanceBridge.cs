using System.ComponentModel.DataAnnotations;
using GDF.Editor;
using GDF.Util;
using Godot;

namespace GDF.Logical.Signals;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/signal_station_auto_instance.png")]
public partial class SignalStationInstanceBridge : SignalStation
{
    private SignalStation _connectedParentStation;

    private SignalStation GetParentOwnedSignalStation(out string error)
    {
        error = null;
        var parent = GetParent();
        if (parent == null)
        {
            error = "No parent set.";
            return null;
        }
        if (string.IsNullOrEmpty(parent.SceneFilePath))
        {
            error = "Parent is not a scene instance.";
            return null;
        }
        var station = GetParent()?.GetChildOfType<SignalStation>();
        if (station?.Owner != GetParent())
        {
            error = $"Parent does not define a {nameof(SignalStation)} node inside its scene.";
            return null;
        }

        if (station == this || station is SignalStationInstanceBridge || station?.Owner == this.Owner)
        {
            error = $"{nameof(SignalStationInstanceBridge)} is meant to be used as a child of a scene instance -- not as a child of the scene root it's defined in.";
            return null;
        }

        return station;
    }
    
    public override void _Notification(int what)
    {
        base._Notification(what);
        switch ((long)what)
        {
            case NotificationParented:
            {
                if (!Engine.IsEditorHint() && _connectedParentStation == null)
                {
                    _connectedParentStation = GetParentOwnedSignalStation(out string error);
                    if (_connectedParentStation != null)
                    {
                        _connectedParentStation.ConnectStationTwoWay(this);
                    }
                    else
                    {
                        GD.PrintErr($"Error setting up {nameof(SignalStationInstanceBridge)}: {error}");
                    }
                }
                break;
            }
            case NotificationUnparented:
            {
                if (_connectedParentStation != null)
                {
                    if(IsInstanceValid(_connectedParentStation)) _connectedParentStation.DisconnectStationTwoWay(this);
                    _connectedParentStation = null;
                }
                break;
            }
        }
    }
    
#if TOOLS
    public override string[] _GetConfigurationWarnings()
    {
        GetParentOwnedSignalStation(out string error);
        if (!string.IsNullOrEmpty(error))
        {
            return new[] { error };
        }
        return base._GetConfigurationWarnings();
    }

    [InspectorCustomControl(AnchorMode = InspectorPropertyAnchorMode.Before)]
    public Control SelectMethod()
    {
        var button = new Button();
        button.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        button.Icon = EditorInterface.Singleton.GetEditorTheme().GetIcon("Tools", "EditorIcons");
        button.Text = "Configure Ports...";
        button.Pressed += ShowPortConfigDialog;
        
        return button;
    }

    private Tree _portConfigTree;

    private void ShowPortConfigDialog()
    {
        var instanceStation = GetParentOwnedSignalStation(out string error);
        if (error != null)
        {
            EditorInterface.Singleton.GetEditorToaster().PushToast(error, EditorToaster.Severity.Error);
            return;
        }
        
        var theme = EditorInterface.Singleton.GetEditorTheme();

        var dialog = new ConfirmationDialog()
        {
            Title = "Configure SignalStation Ports",
            Borderless = false,
            Size = new Vector2I(800, 350),
            Theme = theme,
            Transient = true,
            Exclusive = true
        };
        dialog.CloseRequested += dialog.QueueFree;

        var bg = new MarginContainer()
        {
            AnchorLeft = 0, AnchorTop = 0,
            AnchorRight = 1, AnchorBottom = 1
        };
        dialog.AddChild(bg);

        var vbox = new VBoxContainer()
        {
            AnchorLeft = 0, AnchorTop = 0,
            AnchorRight = 1, AnchorBottom = 1
        };
        bg.AddChild(vbox);

        var content = new VBoxContainer()
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        vbox.AddChild(content);
        var tree = new Tree()
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HideRoot = true,
            HideFolding = true
        };
        content.AddChild(tree);
        tree.Columns = 3;
        var root = tree.CreateItem();

        tree.SetColumnTitlesVisible(true);
        const int colInnerPort = 0;
        const int colOuterPort = 1;
        const int colStatus = 2;
        tree.SetColumnTitle(colInnerPort, $"Ports in {instanceStation.Name}");
        tree.SetColumnTitle(colOuterPort, $"Ports in {this.Name}");
        tree.SetColumnTitle(colStatus, $"Status");
        tree.SetColumnExpandRatio(colStatus, 2);
        foreach (var port in instanceStation.GetChildrenOfType<SignalPort>())
        {
            var oppositeType = port is SignalPortInbound ? typeof(SignalPortOutbound) : typeof(SignalPortInbound);
            
            var matchingPort = this.GetNodeOrNull(new NodePath(port.Name));
            bool alreadyExists = matchingPort != null;
            bool matchingType = alreadyExists && oppositeType.IsAssignableFrom(matchingPort.GetType());
            
            var item = tree.CreateItem();
            item.SetMetadata(0, port);
            item.SetCellMode(colInnerPort, TreeItem.TreeCellMode.Check);
            item.SetText(colInnerPort, port.Name);
            item.SetIcon(colInnerPort, EditorUtils.GetObjectIcon(port));

            item.SetText(colOuterPort, port.Name);
            item.SetIcon(colOuterPort, matchingPort != null ? EditorUtils.GetObjectIcon(matchingPort) : EditorUtils.GetTypeIcon(oppositeType));

            if (!alreadyExists)
            {
                item.SetChecked(colInnerPort, true);
                item.SetEditable(colInnerPort, true);
                item.SetEditable(colOuterPort, false);
                item.SetIcon(colStatus, theme.GetIcon("StatusSuccess", "EditorIcons"));
                item.SetText(colStatus, "Will create");
            }
            else if (!matchingType)
            {
                item.SetChecked(colInnerPort, false);
                item.SetEditable(colInnerPort, false);
                item.SetEditable(colOuterPort, false);
                item.SetIcon(colStatus, theme.GetIcon("StatusError", "EditorIcons"));
                item.SetText(colStatus, "Conflicting port types");
            }
            else
            {
                item.SetChecked(colInnerPort, false);
                item.SetEditable(colInnerPort, false);
                item.SetText(colStatus, "Already exists");
            }
        }

        _portConfigTree = tree;
        dialog.Connect(AcceptDialog.SignalName.Confirmed, new Callable(this, MethodName.PortConfigConfirmed));

        EditorInterface.Singleton.PopupDialogCentered(dialog);
    }

    private void PortConfigConfirmed()
    {
        var tree = _portConfigTree;
        _portConfigTree = null;

        var undoRedo = EditorInterface.Singleton.GetEditorUndoRedo();
        bool actionCreated = false;
        
        foreach (var item in tree.GetRoot().GetChildren())
        {
            if (!item.IsEditable(0)) continue;
            if (!item.IsChecked(0)) continue;

            var innerPort = item.GetMetadata(0).As<SignalPort>();

            if (innerPort == null) continue;

            SignalPort matchingPort =
                innerPort is SignalPortInbound ? new SignalPortOutbound() : new SignalPortInbound();

            matchingPort.Name = innerPort.Name;

            if (!actionCreated)
            {
                undoRedo.CreateAction("Configure SignalStation Ports", customContext: this);
                actionCreated = true;
            }
            
            undoRedo.AddDoReference(matchingPort);
            undoRedo.AddDoMethod(this, Node.MethodName.AddChild, matchingPort);
            undoRedo.AddDoProperty(matchingPort, Node.PropertyName.Owner, this.Owner ?? this);
            
            undoRedo.AddUndoProperty(matchingPort, Node.PropertyName.Owner, Variant.From<Node>(null));
            undoRedo.AddUndoMethod(this, Node.MethodName.RemoveChild, matchingPort);
        }

        if (actionCreated)
            undoRedo.CommitAction();
    }
#endif
}