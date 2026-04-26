using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GDF.Input;

[GlobalClass]
public partial class GdfInputMap : Resource
{
    public const char TagPathSeparator = ':';
    public const char KeyValueSeparator = '=';
    
    private readonly Dictionary<string, GdfInputLocation> _mappings = new();

    public event Action<string, NodePath, GdfInputLocation> MappingSet;
    public event Action<string, NodePath> MappingCleared;

    [Export(PropertyHint.MultilineText)]
    public string Raw
    {
        get => _raw;
        set
        {
            _raw = value;
            _dirty = true;
        }
    }

    private bool _dirty = false;
    private string _raw = "";

    public void EnsureReady()
    {
        if (!_dirty) return;
        _dirty = false;
        ClearMappings();
        foreach (string rawLine in Raw.ReplaceLineEndings("\n").Split('\n'))
        {
            string line = rawLine.Trim();
            if (line.StartsWith('#')) continue; // comment
            if (string.IsNullOrEmpty(line)) continue;
            SetMapping(rawLine);
        }
    }


    public bool TryGetMapping(string contextTag, NodePath path, out GdfInputLocation location)
    {
        EnsureReady();
        string key = CreateKey(contextTag, path);
        return _mappings.TryGetValue(key, out location);
    }

    public bool TryGetMapping(string key, out GdfInputLocation location)
    {
        EnsureReady();
        return _mappings.TryGetValue(key, out location);
    }

    public bool HasMapping(string contextTag, NodePath path)
    {
        EnsureReady();
        string key = CreateKey(contextTag, path);
        return _mappings.ContainsKey(key);
    }

    public bool HasMapping(string key)
    {
        EnsureReady();
        return _mappings.ContainsKey(key);
    }
    
    public void SetMapping(string keyAndValue)
    {
        EnsureReady();
        if(SplitKeyAndValue(keyAndValue, out string key, out var location))
            SetMapping(key, location);
    }
    
    public void SetMapping(string key, GdfInputLocation location)
    {
        EnsureReady();
        if (TryGetMapping(key, out var existing) && existing == location) return;
        if(SplitKey(key, out string contextTag, out var nodePath))
            SetMapping(contextTag, nodePath, location);
    }

    public void SetMapping(string contextTag, NodePath path, GdfInputLocation location)
    {
        EnsureReady();
        string key = CreateKey(contextTag, path);
        if (TryGetMapping(key, out var existing) && existing == location) return;

        _mappings[key] = location;
        MappingSet?.Invoke(contextTag, path, location);
    }
    
    
    
    public void ClearMapping(string key)
    {
        EnsureReady();
        if (!HasMapping(key)) return;
        if (SplitKey(key, out string contextTag, out var nodePath))
            ClearMapping(contextTag, nodePath);
    }

    public void ClearMapping(string contextTag, NodePath path)
    {
        EnsureReady();
        string key = CreateKey(contextTag, path);
        if (!HasMapping(key)) return;

        _mappings.Remove(key);
        MappingCleared?.Invoke(contextTag, path);
    }

    public void ClearMappings()
    {
        EnsureReady();
        string[] keys = _mappings.Keys.ToArray();
        _mappings.Clear();
        foreach (string key in keys)
        {
            if(SplitKey(key, out string contextTag, out var nodePath))
                MappingCleared?.Invoke(contextTag, nodePath);
        }
    }

    private static bool SplitKeyAndValue(string keyAndValue, out string key, out GdfInputLocation location)
    {
        key = default;
        location = default;
        int separatorIndex = keyAndValue.LastIndexOf(KeyValueSeparator);
        if (separatorIndex == -1)
        {
            GD.PrintErr($"[{nameof(GdfInputMap)}] Malformed input map entry '{keyAndValue}', skipping");
            return false;
        }

        string valueCode = keyAndValue[(separatorIndex+1)..];
        if (!GdfInputLocation.TryParse(valueCode, out location))
        {
            GD.PrintErr($"[{nameof(GdfInputMap)}] Malformed value in input map entry '{keyAndValue}', skipping");
            return false;
        }
        key = keyAndValue[..separatorIndex];

        return true;
    }

    private static bool SplitKey(string key, out string contextTag, out NodePath nodePath)
    {
        contextTag = default;
        nodePath = default;
        
        int separatorIndex = key.IndexOf(TagPathSeparator);
        if (separatorIndex == -1)
        {
            contextTag = null;
            nodePath = key;
            return true;
        }

        contextTag = key[..separatorIndex];
        nodePath = key[(separatorIndex + 1)..];
        return true;
    }

    public static string CreateKey(string contextTag, NodePath nodePath)
    {
        if (contextTag == null) return nodePath.ToString();
        return $"{contextTag}{TagPathSeparator}{nodePath}";
    }

    public void DumpMappings(List<(string ContextTag, NodePath NodePath, GdfInputLocation Location)> output)
    {
        EnsureReady();
        foreach ((string key, var location) in _mappings)
        {
            if (SplitKey(key, out string contextTag, out var nodePath))
            {
                output.Add((contextTag, nodePath, location));
            }
        }
    }
}