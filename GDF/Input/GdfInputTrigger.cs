using System;
using Godot;

namespace GDF.Input;

[GlobalClass]
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