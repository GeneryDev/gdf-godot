using Godot;

namespace GDF.Util;

public static class PackedSceneExtensions
{
    public static readonly StringName SignalNameSceneInstantiated = "_scene_instantiated";
    
    /// <summary>
    /// Instantiates the scene's node hierarchy.
    /// Triggers child scene instantiation(s).
    /// <br/>
    /// Triggers a <see cref="Godot.Node.NotificationSceneInstantiated"/> notification on the root node, a custom <see cref="Util.PackedSceneExtensions.NotificationDeepSceneInstantiated"/> notification on all nodes in the instantiated scene, and fires a user signal to the root node (see <see cref="ConnectToSceneInstantiatedSignal"/>).
    /// </summary>
    public static Node GdfInstantiate(this PackedScene scene)
    {
        var instantiated = scene.Instantiate();
        instantiated?.PropagateNotification(GdfConstants.NotificationDeepSceneInstantiated);
        if (instantiated?.HasUserSignal(SignalNameSceneInstantiated) ?? false)
            instantiated.EmitSignal(SignalNameSceneInstantiated);
        return instantiated;
    }
    /// <summary>
    /// Instantiates the scene's node hierarchy, returning null on failure. Triggers child scene instantiation(s).
    /// <br/>
    /// Triggers a <see cref="Godot.Node.NotificationSceneInstantiated"/> notification on the root node, a custom <see cref="Util.PackedSceneExtensions.NotificationDeepSceneInstantiated"/> notification on all nodes in the instantiated scene, and fires a user signal to the root node (see <see cref="ConnectToSceneInstantiatedSignal"/>).
    /// <br/>
    /// Additionally, if the instantiated node is of an incorrect type, it is freed from memory. 
    /// </summary>
    public static T GdfInstantiateOrNull<T>(this PackedScene scene)
    {
        var instantiated = scene.GdfInstantiate();
        if (instantiated is T t) return t;
        instantiated?.Free();
        return default;
    }
    /// <summary>
    /// Instantiates the scene's node hierarchy, returning null on failure. Triggers child scene instantiation(s).
    /// <br/>
    /// Triggers a <see cref="Godot.Node.NotificationSceneInstantiated"/> notification on the root node, a custom <see cref="Util.PackedSceneExtensions.NotificationDeepSceneInstantiated"/> notification on all nodes in the instantiated scene, and fires a user signal to the root node (see <see cref="ConnectToSceneInstantiatedSignal"/>).
    /// <br/>
    /// Additionally, if the instantiated node is of an incorrect type, it is freed from memory, and an error is printed. 
    /// </summary>
    public static T GdfInstantiate<T>(this PackedScene scene)
    {
        var instantiated = scene.GdfInstantiate();
        if (instantiated is T t) return t;
        GD.PrintErr($"Attempted to instantiate scene '{scene?.ResourcePath}' with type {typeof(T).Name}, but instead found {instantiated?.GetType().Name} as the root node");
        instantiated?.Free();
        return default;
    }

    /// <summary>
    /// Connects a callable to a signal on this node that fires whenever <see cref="Node.NotificationParented"/> finishes instantiating the scene with this node as its root.
    /// <br/>
    /// Designed to be called during early notifications such as <see cref="Node"/> and the custom <see cref="Node.Owner"/>, accessing this node via <see cref="Node"/>.
    /// </summary>
    public static Error ConnectToSceneInstantiatedSignal(this Node node, Callable callable, GodotObject.ConnectFlags flags = 0)
    {
        var owner = node;
        while (owner?.Owner is { } newOwner) owner = newOwner;
        
        if (string.IsNullOrEmpty(owner?.SceneFilePath)) return Error.Failed;
        if (!owner.HasUserSignal(SignalNameSceneInstantiated))
        {
            owner.AddUserSignal(SignalNameSceneInstantiated);
        }

        return owner.Connect(SignalNameSceneInstantiated, callable, (uint)flags);
    }
}