using System;

namespace GDF.Debug;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class HasDebugCommandsAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method)]
public class DebugCommandAttribute : Attribute
{
    public string Id;
    public DebugCommandType Type;

    public DebugCommandAttribute(string id, DebugCommandType type = DebugCommandType.Trigger)
    {
        Id = id;
        Type = type;
    }
}

public enum DebugCommandType
{
    Trigger,
    Toggle
}
