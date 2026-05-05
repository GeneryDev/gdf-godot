using Godot;

namespace GDF.PropertyStacks;

[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/property_stack.png")]
public partial class GlobalPropertyStack : PropertyStack
{
    public static GlobalPropertyStack Instance
    {
        get
        {
            if (_instance == null)
                if (!Engine.IsEditorHint())
                    GD.PushError(
                        $"There's no singleton node instance for '{nameof(GlobalPropertyStack)}'. Make sure to add one to autoload.");
            if (Engine.IsEditorHint() && !IsInstanceValid(_instance)) _instance = null;
            return _instance;
        }
    }

    public static bool InstanceExists => _instance != null;

    private static GlobalPropertyStack _instance;

    public override void _EnterTree()
    {
        _instance = this;
        base._EnterTree();
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        if (_instance == this && !Engine.IsEditorHint())
            _instance = null;
    }
}