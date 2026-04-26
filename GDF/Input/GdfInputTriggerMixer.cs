using GDF.Util;
using Godot;

namespace GDF.Input;

[Tool]
[GlobalClass]
public partial class GdfInputTriggerMixer : GdfInputTrigger
{
    [Export] public float LimitLength = 1.0f;
    
    public override EventMatchResult MatchEvent(GdfPlayerInput player, InputEvent evt)
    {
        var strongestMatch = EventMatchResult.NoChange;
        foreach (var trigger in this.IterateChildrenOfType<GdfInputTrigger>())
        {
            strongestMatch |= trigger.MatchEvent(player, evt);
            
            if (strongestMatch == EventMatchResult.StrongChange) // can exit early
                return strongestMatch;
        }

        return strongestMatch;
    }

    public override GdfPlayerInput.InputActionState GetCurrentState(GdfPlayerInput player)
    {
        var currentValue = Vector3.Zero;
        foreach (var trigger in this.IterateChildrenOfType<GdfInputTrigger>())
        {
            var componentState = trigger.GetCurrentState(player);
            currentValue += componentState.AxisValue;
        }

        if (LimitLength > 0) currentValue = currentValue.LimitLength(LimitLength);

        return new GdfPlayerInput.InputActionState()
        {
            AxisValue = currentValue,
            BoolValue = !currentValue.IsZeroApprox(),
            Strength = Mathf.Clamp(currentValue.Length(), 0, 1)
        };
    }
}