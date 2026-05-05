using Godot;

namespace GDF.Util;

public interface ISingletonNode<T> where T : Node
{
    public static T Instance
    {
        get
        {
            if (_instance == null)
                if (!Engine.IsEditorHint())
                    GD.PushError(
                        $"There's no singleton node instance for '{typeof(T).Name}'. Make sure to add one to autoload.");
            if (Engine.IsEditorHint() && !GodotObject.IsInstanceValid(_instance)) _instance = null;
            return _instance;
        }
    }

    public static bool InstanceExists => _instance != null;

    private static T _instance;
}