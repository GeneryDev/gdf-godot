#if TOOLS
using GDF.Animations;
using GDF.Editor;
using Godot;

namespace GDF;

[Tool]
public partial class GdfEditorPlugin : EditorPlugin
{
    public static GdfEditorPlugin Instance;
    
    public AnimationPlayerEditor AnimationEditor;

    private ToolInspectorPlugin _toolInspectorPlugin;
    private AnimationInspectorPlugin _animationInspectorPlugin;
    
    private AnimationMixer _editingAnimationMixer;
    private bool _editingMixerUnselectedButStillInTree = false;

    public GdfEditorPlugin()
    {
        Instance = this;
    }
    
    public override void _EnterTree()
    {
        Instance = this;
        AddInspectorPlugin(_toolInspectorPlugin = new ToolInspectorPlugin());
        AddInspectorPlugin(_animationInspectorPlugin = new AnimationInspectorPlugin());
        
        AnimationEditor ??= new AnimationPlayerEditor();
        
        GD.Print("Initialized Genery Development Framework Editor Plugin");
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
        RemoveInspectorPlugin(_toolInspectorPlugin);
        RemoveInspectorPlugin(_animationInspectorPlugin);
    }

    public override void _Process(double delta)
    {
        SanitizeEditingMixer();
        if (!AnimationEditor.IsValid)
        {
            AnimationEditor.SetEditorControl(FindAnimationPlayerEditor());
        }
        AnimationEditor?._Process(delta);
    }

    public Control FindAnimationPlayerEditor()
    {
#if GODOT4_6_OR_GREATER
        var dummyControl = new EditorDock()
        {
            DefaultSlot = EditorDock.DockSlot.Bottom,
            AvailableLayouts = EditorDock.DockLayout.Horizontal
        };
        AddDock(dummyControl);
#else
        var dummyControl = new Control();
        AddControlToBottomPanel(dummyControl, "Find animation editor pls");
#endif
        try
        {
            foreach (var sibling in dummyControl.GetParentControl().GetChildren())
            {
                if (sibling.GetClass() != "AnimationPlayerEditor") continue;
                return (Control)sibling;
            }
        }
        finally
        {
#if GODOT4_6_OR_GREATER
            RemoveDock(dummyControl);
#else
            RemoveControlFromBottomPanel(dummyControl);
#endif
            dummyControl.QueueFree();
        }
	
        return null;
    }

    public override bool _Handles(GodotObject @object)
    {
        if (@object is AnimationMixer) return true;
        return false;
    }

    public override void _Edit(GodotObject @object)
    {
        if (@object != null)
        {
            _editingAnimationMixer = (AnimationMixer)@object;
            GD.Print($"Editing animation mixer: {_editingAnimationMixer}");
            _editingMixerUnselectedButStillInTree = false;
        } else if (_editingAnimationMixer.IsInsideTree())
        {
            // Keep last edited animation mixer, it's still valid
            _editingMixerUnselectedButStillInTree = true;
            GD.Print("Deselected mixer, but still in tree");
        }
        else
        {
            _editingAnimationMixer = null;
            GD.Print("No animation mixer active");
            _editingMixerUnselectedButStillInTree = false;
        }
    }

    private void SanitizeEditingMixer()
    {
        if (_editingMixerUnselectedButStillInTree && (!IsInstanceValid(_editingAnimationMixer) || !_editingAnimationMixer.IsInsideTree()))
        {
            _editingAnimationMixer = null;
            _editingMixerUnselectedButStillInTree = false;
            GD.Print("Deselected mixer removed from tree");
        }
    }
	
    public AnimationMixer GetEditingAnimationMixer()
    {
        SanitizeEditingMixer();
        return _editingAnimationMixer;
    }
}
#endif
