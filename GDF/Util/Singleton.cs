using System;
using Godot;

namespace GDF.Util;

[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/simple.png")]
public abstract partial class Singleton : Node
{
}

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class SingletonUsageAttribute : Attribute
{
    public SingletonUsage Usage;
    public bool SuppressMissingOnAccessError;

    public SingletonUsageAttribute()
    {
        Usage = SingletonUsage.Autoload;
    }
    
    public SingletonUsageAttribute(SingletonUsage usage)
    {
        Usage = usage;
    }
}

public enum SingletonUsage
{
    Autoload,
    Scene
}