using System.Collections.Generic;
using Godot;

namespace GDF.Util;

public struct CallableEventDictionary<TKey>
{
    private Dictionary<TKey, CallableEvent> _events;

    public void Connect(TKey key, Callable callable)
    {
        _events ??= new();
        if (!_events.TryGetValue(key, out var events))
        {
            events = new CallableEvent();
        }
        events.Connect(callable);
        _events[key] = events;
    }
    public void Disconnect(TKey key, Callable callable)
    {
        if (_events == null) return;
        if (!_events.TryGetValue(key, out var events))
        {
            events = new CallableEvent();
        }
        events.Disconnect(callable);
        _events[key] = events;
    }

    public void Invoke(TKey key, params Variant[] args)
    {
        if (_events == null) return;
        if (_events.TryGetValue(key, out var events))
        {
            events.Invoke(args);
        }
    }
}