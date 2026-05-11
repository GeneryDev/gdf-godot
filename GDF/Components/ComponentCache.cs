using GDF.Util;
using Godot;

namespace GDF.Components;

public struct ComponentCache<T> where T : class
{
    private Node _source;
    private T _cache;
    private bool _everRequestedInTree;

    public ComponentCache()
    {
    }

    public ComponentCache(Node source)
    {
        _source = source;
    }

    public void Clear()
    {
        _source = null;
        _cache = null;
        _everRequestedInTree = false;
    }
    
    public T Get(Node source)
    {
        if (_source != source) Clear();
        _source = source;
        if (_cache != null || _everRequestedInTree) return _cache;
        if (source == null) return null;
        _cache = _source.GetComponent<T>();
        _everRequestedInTree |= source.IsInsideTree();
        return _cache;
    }
}