using Godot;

namespace GDF.Util;

public static class GodotObjectExtensions
{
    /// <summary>
    /// Connects a signal by name to a given callable, if the connection does not already exist.
    /// </summary>
    public static bool TryConnect(this GodotObject obj, StringName signal, Callable callable, GodotObject.ConnectFlags flags = 0)
    {
        if (obj == null || !GodotObject.IsInstanceValid(obj)) return false;
        if (!obj.IsConnected(signal, callable))
        {
            obj.Connect(signal, callable, (uint)flags);
            return true;
        }

        return false;
    }
    /// <summary>
    /// Disconnects a signal by name from a given callable, if the connection exists.
    /// </summary>
    public static bool TryDisconnect(this GodotObject obj, StringName signal, Callable callable)
    {
        if (obj == null || !GodotObject.IsInstanceValid(obj)) return false;
        if (obj.IsConnected(signal, callable))
        {
            obj.Disconnect(signal, callable);
            return true;
        }

        return false;
    }
}