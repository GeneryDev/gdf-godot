using System;
using System.Reflection;
using Godot;
// ReSharper disable StaticMemberInGenericType

namespace GDF.Util;

public abstract partial class SingletonNode<T> : Singleton where T : SingletonNode<T>
{
    public static readonly CallableEvent InstanceChangedEvent = new();
    
    public static T Instance
    {
        get
        {
            if (_instance == null)
                if (!Engine.IsEditorHint() && !UsageAttribute.SuppressMissingOnAccessError)
                {
                    switch (UsageAttribute.Usage)
                    {
                        case SingletonUsage.Autoload:
                            GD.PushError($"There's no singleton node instance for '{typeof(T).Name}'. " +
                                         $"Make sure to add one to autoload.");
                            break;
                        case SingletonUsage.Scene:
                            GD.PushError($"There's no singleton node instance for '{typeof(T).Name}'. " +
                                         $"Make sure to add one to the tree.");
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            
            if (Engine.IsEditorHint() && !IsInstanceValid(_instance)) _instance = null;
            return _instance;
        }
    }

    public static bool InstanceExists => _instance != null;
    
    public static SingletonUsageAttribute UsageAttribute
    {
        get
        {
            if (_usageAttribute == null)
            {
                _usageAttribute = typeof(T).GetCustomAttribute<SingletonUsageAttribute>() ?? new SingletonUsageAttribute();
            }
            return _usageAttribute;
        }
    }

    private static T _instance;
    private static SingletonUsageAttribute _usageAttribute;

    public override void _EnterTree()
    {
        _instance = (T)this;
        InstanceChangedEvent.Invoke();
    }

    public override void _ExitTree()
    {
        if (_instance == this && !Engine.IsEditorHint())
        {
            _instance = null;
            InstanceChangedEvent.Invoke();
        }
    }
}