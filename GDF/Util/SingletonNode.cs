using Godot;

namespace GDF.Util;

public abstract partial class SingletonNode<T> : Node where T : SingletonNode<T>
{
    public static T Instance
    {
        get
        {
            if (_instance == null)
                if (!Engine.IsEditorHint())
                    GD.PushError(
                        $"There's no singleton node instance for '{typeof(T).Name}'. Make sure to add one to autoload.");
            if (Engine.IsEditorHint() && !IsInstanceValid(_instance)) _instance = null;
            return _instance;
        }
    }

    public static bool InstanceExists => _instance != null;

    private static T _instance;

    public override void _EnterTree()
    {
        _instance = (T)this;
    }

    public override void _ExitTree()
    {
        if (_instance == this && !Engine.IsEditorHint())
            _instance = null;
    }
}