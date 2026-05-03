using GDF.Util;
using Godot;

namespace GDF.Components;

public struct ComponentCache<T> where T : class
{
    private Node _source;
    private T _cache;

    public ComponentCache()
    {
    }

    public ComponentCache(Node source)
    {
        _source = source;
    }
    
    public T Get(Node source)
    {
        if (_source != source) _cache = null;
        _source = source;
        if (_cache != null) return _cache;
        if (source == null) return null;
        _cache = _source.GetComponent<T>();
        return _cache;
    }
}