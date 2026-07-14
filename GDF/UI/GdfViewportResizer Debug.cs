using GDF.Debug;
using Godot;

namespace GDF.UI;

[HasDebugCommands]
public partial class GdfViewportResizer
{
    [DebugCommand("gdf:fullscreen")]
    public static void DebugFullscreen()
    {
        Instance?.ToggleFullscreen();
    }
    [DebugCommand("gdf:resolution", DebugCommandType.TriggerWithArguments)]
    public static void DebugResolution(DebugCommandArgumentParser args)
    {
        if (args.ReachedEnd())
        {
            GD.Print($"Current resolution: {Instance.UserSettings.Resolution}");
        }
        else if (args.ReadInt(out int x) && args.ReadInt(out int y))
        {
            Instance.UserSettings.Resolution = new Vector2I(x, y);
            GD.Print($"Changing resolution to: {Instance.UserSettings.Resolution}");
        }
        else
        {
            args.PrintError();
        }
    }
}