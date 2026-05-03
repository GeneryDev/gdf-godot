#if TOOLS
using Godot;

namespace GDF.Editor;

public static partial class EditorUtils
{
    public static bool IsSettingPropertyThroughInspector(Node node)
    {
        return node != null &&
               Engine.IsEditorHint() &&
               node.GetParent() != null &&
               node.IsPartOfEditedScene() &&
               EditorInterface.Singleton?.GetInspector()
                   ?.GetEditedObject() == node;
    }
}
#endif
