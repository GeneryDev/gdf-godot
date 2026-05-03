using System.Collections.Generic;
using GDF.Util;
using Godot;

namespace GDF.Input;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/input_action_instance.png")]
public partial class GdfInputActionInstance : Node
{
    [Export]
    public GdfInputAction Action;

    private readonly List<GdfInputTrigger> _triggers = new();

    public override void _EnterTree()
    {
        if (!Engine.IsEditorHint())
        {
            this.ChildEnteredTree += OnChildEnteredTree;
            this.ChildExitingTree += OnChildExitingTree;
        }
    }

    public override void _ExitTree()
    {
        if (!Engine.IsEditorHint())
        {
            this.ChildEnteredTree -= OnChildEnteredTree;
            this.ChildExitingTree -= OnChildExitingTree;
        }
    }

    private void OnChildEnteredTree(Node node)
    {
        if(node is GdfInputTrigger trigger) _triggers.Add(trigger);
    }

    private void OnChildExitingTree(Node node)
    {
        if(node is GdfInputTrigger trigger) _triggers.Remove(trigger);
    }

    public void TickPlayer(GdfPlayerInput player)
    {
        // foreach (var trigger in _triggers)
        // {
        // }
    }
    public void HandleInput(GdfPlayerInput player, InputEvent evt, GdfInputContext context)
    {
        foreach (var trigger in _triggers)
        {
            var matchResult = trigger.MatchEvent(player, evt);

            if ((matchResult & GdfInputTrigger.EventMatchResult.ShouldUpdateState) != 0)
            {
                player.QueueUpdateAction(Action, evt);
                GetWindow().SetInputAsHandled();
            }
            if ((matchResult & GdfInputTrigger.EventMatchResult.ShouldNotifyUsed) != 0)
            {
                player.NotifyUsed(Action, context, evt);
            }
        }
    }

    public GdfPlayerInput.InputActionState GetActionState(GdfPlayerInput player)
    {
        GdfPlayerInput.InputActionState maxStrengthState = default;
        foreach (var trigger in this.IterateChildrenOfType<GdfInputTrigger>())
        {
            var state = trigger.GetCurrentState(player);
            if (state.Strength >= maxStrengthState.Strength) maxStrengthState = state;
        }

        return maxStrengthState;
    }
}