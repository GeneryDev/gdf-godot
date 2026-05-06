using Godot;

namespace GDF.Debug;

[GlobalClass]
public partial class DebugCommandMacro : Resource
{
    [Export] public InputEventKey Shortcut;
    [Export] public Key RequireKeyPressed = Key.None;
    [Export] public string Command;
}