using System.Text;
using Godot;
using static GDF.Animations.GdfAnimationEventAction;

namespace GDF.Animations;

public enum GdfAnimationEventAction
{
    None = 0,
    // State
    StateStart = 1,
    StateTravel = 2,
    StateRestart = 3,
    StateTravelOrRestart = 4,
    // One Shot
    OneShotRequestFire = 16,
    OneShotRequestAbort = 17,
    OneShotRequestFadeOut = 18
}

public static class GdfAnimationEventActions
{
    public static string GetAnimationEventActionOptionsString(string attachedClassName)
    {
        var sb = new StringBuilder();

        sb.Append($"<none selected>:{None:D}");

        if (attachedClassName == nameof(AnimationNodeTransition))
        {
            sb.Append($",Transition · Travel:{StateTravel:D}");
        }

        if (attachedClassName is nameof(AnimationNodeAnimation) or nameof(AnimationNodeBlendSpace1D)
            or nameof(AnimationNodeBlendSpace2D) or nameof(AnimationNodeBlendTree) or nameof(AnimationNodeStateMachine))
        {
            sb.Append($",State · Start:{StateStart:D},State · Travel:{StateTravel:D},State · Restart:{StateRestart:D},State · Travel Or Restart:{StateTravelOrRestart:D}");
        }

        if (attachedClassName == nameof(AnimationNodeOneShot))
        {
            sb.Append($",OneShot · Fire:{OneShotRequestFire:D},OneShot · Abort:{OneShotRequestAbort:D},OneShot · Fade Out:{OneShotRequestFadeOut:D}");
        }

        return sb.ToString();
    }
}