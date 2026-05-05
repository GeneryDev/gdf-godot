#if TOOLS
using Godot;

namespace GDF.Editor;

public static class AnimationEditorUtil
{
    public static AnimationPlayerEditor GetAnimationPlayerEditor()
    {
        return AnimationPlayerEditor.Instance;
    }
	
    public static AnimationMixer GetEditingAnimationMixer()
    {
        return GdfEditorPlugin.Instance?.GetEditingAnimationMixer();
    }
}
#endif
