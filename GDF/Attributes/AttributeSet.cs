using System;
using System.Collections.Generic;
using GDF.Attributes.Impl;

namespace GDF.Attributes;

public class AttributeSet<TKey> : IAttributeSet<TKey>
{
    private readonly Dictionary<TKey, IAttribute> _attributes = new();
    
    public void RegisterAttribute(TKey id, IAttribute attribute)
    {
        if (!_attributes.TryAdd(id, attribute))
        {
            GdfLogger.Error($"An attribute with ID '{id}' has already been registered to this AttributeSet");
        }
    }

    public bool UnregisterAttribute(TKey id)
    {
        return _attributes.Remove(id);
    }

    public bool HasAttribute(TKey id)
    {
        return _attributes.ContainsKey(id);
    }

    public void CheckIntegrity(IntegrityCheckParams param)
    {
        foreach (var (_, attr) in _attributes)
        {
            attr.CheckIntegrity(param);
        }
    }

    public IAttribute GetAttribute(TKey id)
    {
        if (!_attributes.TryGetValue(id, out var attr))
        {
            GdfLogger.Error($"No such attribute with ID '{id}' in this AttributeSet");
            return null;
        }

        return attr;
    }

    public bool IsAttribute<T>(TKey id)
    {
        return GetAttribute(id) is T;
    }

    public bool IsAttributeComputable<T>(TKey id)
    {
        return IsAttribute<IAttributeComputable<T>>(id);
    }

    public bool IsAttributeModifiable<T>(TKey id)
    {
        return IsAttribute<IModifiable<T>>(id);
    }

    public T GetValue<T>(TKey id)
    {
        var attr = GetAttribute(id);
        if (attr == null) return default;
        if (attr is IAttributeComputable<T> computable)
        {
            return computable.CurrentValue;
        }
        GdfLogger.Error($"Attribute with ID '{id}' cannot be computed as {typeof(T).Name}");
        return default;
    }
}

public partial class BulkModifier
{
    public string Description;
    public float DefaultOrder;

    private float _weight = 1.0f;
    private IntegrityChecker _integrityChecker;

    public float Weight
    {
        get => _weight;
        set => SetWeight(value);
    }
    public IntegrityChecker IntegrityChecker
    {
        get => _integrityChecker;
        set => SetIntegrityChecker(value);
    }

    private readonly List<(IAttribute Attribute, ModifierToken Token)> _modifierTokens = new();

    public BulkModifier(string description, float defaultOrder = 0)
    {
        Description = description;
        DefaultOrder = defaultOrder;
    }

    public BulkModifier Modify<T>(IAttribute attr, T value)
    {
        if (attr == null) return this;
        if (attr is not IModifiable<T> modifiable)
        {
            GdfLogger.Error($"Attribute {attr} is not modifiable using type {typeof(T).Name}");
            return this;
        }

        foreach (var existing in _modifierTokens)
        {
            if (existing.Attribute != attr) continue;
            
            // existing
            modifiable.Modify(existing.Token, value);
            return this;
        }
        
        // new token

        var token = attr.CreateToken(Description, DefaultOrder);
        _modifierTokens.Add((attr, token));
        modifiable.Modify(token, value);
        SyncNewToken(token);

        return this;
    }

    public BulkModifier SetWeight(float weight)
    {
        _weight = weight;
        foreach(var entry in _modifierTokens)
        {
            entry.Token.SetWeight(weight);
        }
        return this;
    }

    public BulkModifier SetIntegrityChecker(IntegrityChecker integrityChecker)
    {
        _integrityChecker = integrityChecker;
        foreach(var entry in _modifierTokens)
        {
            entry.Token.SetIntegrityChecker(integrityChecker);
        }
        return this;
    }

    private void SyncNewToken(in ModifierToken token)
    {
        token.SetWeight(_weight);
        token.SetIntegrityChecker(_integrityChecker);
    }

    public bool ModifiesAttribute(IAttribute attr)
    {
        foreach (var entry in _modifierTokens)
        {
            if (entry.Attribute == attr) return true;
        }

        return false;
    }

    public BulkModifier Clear(IAttribute attr)
    {
        for (var index = 0; index < _modifierTokens.Count; index++)
        {
            var entry = _modifierTokens[index];
            if (entry.Attribute == attr)
            {
                entry.Token.Target.ClearModification(entry.Token);
                _modifierTokens.RemoveAt(index);
                index--;
            }
        }

        return this;
    }

    public BulkModifier Clear()
    {
        foreach(var entry in _modifierTokens)
        {
            entry.Token.Target.ClearModification(entry.Token);
        }
        _modifierTokens.Clear();
        
        return this;
    }

    public BulkModifier Discard()
    {
        Clear();
        return null;
    }

    public ModifierToken? GetTokenForAttribute(IAttribute attr)
    {
        foreach(var entry in _modifierTokens)
        {
            if (entry.Attribute == attr) return entry.Token;
        }

        return null;
    }
}

public static class AttributeSetExtensions
{
    public static bool TryModify<T>(this BulkModifier bulkModifier, IAttribute attr, T value)
    {
        if (attr is IModifiable<T>)
        {
            bulkModifier.Modify(attr, value);
            return true;
        }

        return false;
    }
}