using System.Collections.Generic;
using System.Text;
using Godot;
using Godot.Collections;

namespace GDF.Input;

[Tool]
[GlobalClass]
public partial class GdfInputContext : Node
{
    [Export]
    public Array<string> Tags;

    private readonly List<GdfInputActionInstance> _actionInstances = new();
    private readonly Godot.Collections.Array<NodePath> _appliedMappings = new();

    public override void _EnterTree()
    {
        base._EnterTree();
        if (Engine.IsEditorHint()) return;
        Connect(Node.SignalName.ChildEnteredTree,
            new Callable(this, MethodName.OnChildEnteredTree));
        Connect(Node.SignalName.ChildExitingTree,
            new Callable(this, MethodName.OnChildExitingTree));
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        if (Engine.IsEditorHint()) return;
        Disconnect(Node.SignalName.ChildEnteredTree,
            new Callable(this, MethodName.OnChildEnteredTree));
        Disconnect(Node.SignalName.ChildExitingTree,
            new Callable(this, MethodName.OnChildExitingTree));
    }

    public void OnChildEnteredTree(Node node)
    {
        if (node is GdfInputActionInstance actionInstance)
            _actionInstances.Add(actionInstance);
    }

    public void OnChildExitingTree(Node node)
    {
        if (node is GdfInputActionInstance actionInstance)
            _actionInstances.Remove(actionInstance);
    }
    
    
    public bool HasTag(string tag)
    {
        return Tags?.Contains(tag) ?? false;
    }
    
    public void TickPlayer(GdfPlayerInput player)
    {
        foreach (var instance in _actionInstances)
        {
            instance.TickPlayer(player);
        }
    }
    public void HandleInput(GdfPlayerInput player, InputEvent evt)
    {
        foreach (var instance in _actionInstances)
        {
            instance.HandleInput(player, evt, this);
        }
    }
    public void MergeActionStates(GdfPlayerInput player)
    {
        foreach (var instance in _actionInstances)
        {
            player.MergeActionState(instance.Action, instance.GetActionState(player));
        }
    }

    public GdfPlayerInput.InputActionState GetActionState(GdfPlayerInput player, GdfInputAction action)
    {
        GdfPlayerInput.InputActionState maxStrengthState = default;
        foreach (var instance in _actionInstances)
        {
            if (instance.Action == action)
            {
                var state = instance.GetActionState(player);
                if (state.Strength >= maxStrengthState.Strength) maxStrengthState = state;
            }
        }

        return maxStrengthState;
    }

    public void ClearMappings()
    {
        foreach (var path in _appliedMappings)
        {
            if (this.GetNodeOrNull<GdfInputTriggerInput>(path) is { } trigger)
            {
                trigger.ClearMapping();
            }
        }
        _appliedMappings.Clear();
    }

    public void ApplyMapping(NodePath nodePath, GdfInputLocation location)
    {
        if (!_appliedMappings.Contains(nodePath)) _appliedMappings.Add(nodePath);
        if (this.GetNodeOrNull<GdfInputTriggerInput>(nodePath) is { } trigger)
        {
            trigger.ApplyMapping(location);
        }
    }

    public void ClearMapping(NodePath nodePath)
    {
        if (this.GetNodeOrNull<GdfInputTriggerInput>(nodePath) is { } trigger)
        {
            trigger.ClearMapping();
        }
        _appliedMappings.Remove(nodePath);
    }

#if TOOLS
    [ExportToolButton("Export Input Map Entries")]
    private Callable ButtonExportInputMapEntries => new Callable(this, MethodName.ExportInputMapEntries);

    private void ExportInputMapEntries()
    {
        var inputNodes = this.FindChildren("*", nameof(GdfInputTriggerInput), recursive: true, owned: true);

        var tagsForTabs = new List<string>();
        tagsForTabs.Add(null);
        if(Tags != null) tagsForTabs.AddRange(Tags);

        var tabContainer = new TabContainer();
        var sb = new StringBuilder();
        foreach (string tag in tagsForTabs)
        {
            sb.Clear();
            foreach (var node in inputNodes)
            {
                var inputNode = node as GdfInputTriggerInput;
                if (inputNode == null) continue;
                var path = GetPathTo(inputNode);
                if (tag != null)
                {
                    sb.Append(tag);
                    sb.Append(GdfInputMap.TagPathSeparator);
                }

                sb.Append(path);
                sb.Append(GdfInputMap.KeyValueSeparator);
                sb.AppendLine(inputNode.GetInputLocation().ToString());
            }

            var text = sb.ToString();
            sb.Clear();
            
            var textBox = new CodeEdit();
            textBox.Name = tag ?? "(no tag)";
            
            tabContainer.AddChild(textBox);
            textBox.Text = text;
            textBox.Editable = false;
            textBox.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        }
        
        var window = new Window();
        window.Title = $"Input Map entries for {SceneFilePath}";
        window.Theme = EditorInterface.Singleton.GetEditorTheme();
        window.AddChild(tabContainer);

        tabContainer.AnchorsPreset = (int)Control.LayoutPreset.FullRect;

        window.Transient = true;
        window.PopupWindow = true;
        window.CloseRequested += window.QueueFree;
        EditorInterface.Singleton.PopupDialogCentered(window, new Vector2I(600, 400));
    }
#endif
}