using System.Collections.Generic;
using Godot;

namespace GDF.Util;

public struct CallableEvent
{
    private static readonly List<Callable> _callables = new();
    
    public void Connect(Callable callable)
    {
        _callables.Add(callable);
    }

    public void Disconnect(Callable callable)
    {
        _callables.Remove(callable);
    }

    public void Invoke(params Variant[] args)
    {
        for (var index = 0; index < _callables.Count; index++)
        {
            var callable = _callables[index];
            callable.Call(args);
        }
    }

    public void Invoke()
    {
        for (var index = 0; index < _callables.Count; index++)
        {
            var callable = _callables[index];
            callable.Call();
        }
    }
}