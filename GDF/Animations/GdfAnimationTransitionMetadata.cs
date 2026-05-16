using Godot;

namespace GDF.Animations;

[Tool]
[GlobalClass]
public partial class GdfAnimationTransitionMetadata : Resource
{
    [Export]
    public string[] TriggeringEvents;

    public static bool Supports(GodotObject obj)
    {
        return obj?.IsClass("AnimationNodeStateMachineTransition") ?? false;
    }
}