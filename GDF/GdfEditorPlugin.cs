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
        var dummyControl = new Control();
        AddControlToBottomPanel(dummyControl, "Find animation editor pls");
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
            RemoveControlFromBottomPanel(dummyControl);
            dummyControl.QueueFree();
        }
	
        return null;
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
