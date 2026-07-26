using GDF.Debug;

namespace GDF.Scenes.Transitions;

[HasDebugCommands]
public partial class ScreenTransitionSystem
{
    [DebugCommand("gdf:screen_transition", DebugCommandType.TriggerWithArguments)]
    public static void TestScreenTransition(DebugCommandArgumentParser args)
    {
        if (args.ReadWord(out var transitionId))
            Instance.StartTransition(new ScreenTransitionReference()
            {
                TransitionId = transitionId
            }, default, default, default);
    }
}