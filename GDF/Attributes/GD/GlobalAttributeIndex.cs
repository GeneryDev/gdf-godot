using GDF.Util;
using Godot;

namespace GDF.Attributes;

[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/attribute_index.svg")]
[SingletonUsage(SingletonUsage.Autoload)]
public partial class GlobalAttributeIndex : AttributeIndex
{
    public static GlobalAttributeIndex Instance
    {
        get
        {
            if (_instance == null)
                if (!Engine.IsEditorHint())
                    GD.PushError(
                        $"There's no singleton node instance for '{nameof(GlobalAttributeIndex)}'. Make sure to add one to autoload.");
            if (Engine.IsEditorHint() && !IsInstanceValid(_instance)) _instance = null;
            return _instance;
        }
    }

    public static bool InstanceExists => _instance != null;

    private static GlobalAttributeIndex _instance;

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