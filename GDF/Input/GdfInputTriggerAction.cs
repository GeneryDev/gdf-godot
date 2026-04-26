using Godot;

namespace GDF.Input;

[Tool]
[GlobalClass]
public partial class GdfInputTriggerAction : GdfInputTrigger
{
    [Export] public GdfInputAction Action;
    
    public override EventMatchResult MatchEvent(GdfPlayerInput player, InputEvent evt)
    {
        return EventMatchResult.NoChange;
    }

    public override GdfPlayerInput.InputActionState GetCurrentState(GdfPlayerInput player)
    {
        if (Action == null) return default;
        var state = player.GetActionState(Action);
        return new GdfPlayerInput.InputActionState()
        {
            BoolValue = state.BoolValue,
            Strength = state.Strength,
            AxisValue = state.AxisValue
        };
    }
}