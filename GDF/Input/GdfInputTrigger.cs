using System;
using Godot;

namespace GDF.Input;

[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/input_trigger.png")]
public abstract partial class GdfInputTrigger : Node
{
    public abstract EventMatchResult MatchEvent(GdfPlayerInput player, InputEvent evt);
    public abstract GdfPlayerInput.InputActionState GetCurrentState(GdfPlayerInput player);

    [Flags]
    public enum EventMatchResult
    {
        ShouldUpdateState = 1,
        ShouldNotifyUsed = 2,
        
        NoChange = 0,
        WeakChange = ShouldUpdateState,
        StrongChange = ShouldUpdateState | ShouldNotifyUsed
    }
}