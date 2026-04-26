#if TOOLS
using GDF.Editor;
using Godot;

namespace GDF;

[Tool]
public partial class GdfEditorPlugin : EditorPlugin
{
    private ToolInspectorPlugin _toolInspectorPlugin;
    
    public override void _EnterTree()
    {
        GD.Print("Initialized Genery Development Framework Editor Plugin");
        AddInspectorPlugin(_toolInspectorPlugin = new ToolInspectorPlugin());
    }

    public override void _ExitTree()
    {
        RemoveInspectorPlugin(_toolInspectorPlugin);
    }
}
#endif
