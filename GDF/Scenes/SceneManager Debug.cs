using GDF.Debug;
using GDF.IO;

namespace GDF.Scenes;

[HasDebugCommands]
public partial class SceneManager
{
    [DebugCommand("gdf:scene", DebugCommandType.TriggerWithArguments)]
    public static void TestSceneTransition(DebugCommandArgumentParser args)
    {
        if (args.ReadWord(out var path))
            SceneManager.TransitionToScene(new SceneChangeRequest()
            {
                SceneReference = new ResourceReference()
                {
                    StoredResourcePath = path
                }
            }, null, true);
    }
}