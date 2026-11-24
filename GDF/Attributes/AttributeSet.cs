using System;
using System.Collections.Generic;

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

    public AttributeSetModifier<TKey> CreateSetModifier(string description, float order = 0)
    {
        return new AttributeSetModifier<TKey>(this, description, order);
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

public partial class AttributeSetModifier<TKey>
{
    public IAttributeSet<TKey> AttributeSet;
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

    private readonly Dictionary<TKey, ModifierToken> _modifierTokens = new();

    public AttributeSetModifier(AttributeSet<TKey> attributeSet, string description, float defaultOrder)
    {
        AttributeSet = attributeSet;
        Description = description;
        DefaultOrder = defaultOrder;
    }

    public AttributeSetModifier<TKey> Modify<T>(TKey id, T value)
    {
        var attr = AttributeSet.GetAttribute(id);
        if (attr == null) return this;
        if (attr is not IModifiable<T> modifiable)
        {
            GdfLogger.Error($"Attribute with ID '{id}' is not modifiable using type {typeof(T).Name}");
            return this;
        }

        if (_modifierTokens.TryGetValue(id, out var token))
        {
            modifiable.Modify(token, value);
        }
        else
        {
            token = attr.CreateToken(Description, DefaultOrder);
            _modifierTokens[id] = token;
            modifiable.Modify(token, value);
            SyncNewToken(token);
        }

        return this;
    }

    public AttributeSetModifier<TKey> SetWeight(float weight)
    {
        _weight = weight;
        foreach(var token in _modifierTokens.Values)
        {
            token.SetWeight(weight);
        }
        return this;
    }

    public AttributeSetModifier<TKey> SetIntegrityChecker(IntegrityChecker integrityChecker)
    {
        _integrityChecker = integrityChecker;
        foreach(var token in _modifierTokens.Values)
        {
            token.SetIntegrityChecker(integrityChecker);
        }
        return this;
    }

    private void SyncNewToken(in ModifierToken token)
    {
        token.SetWeight(_weight);
        token.SetIntegrityChecker(_integrityChecker);
    }

    public bool ModifiesAttribute(TKey id)
    {
        return _modifierTokens.ContainsKey(id);
    }

    public AttributeSetModifier<TKey> Clear(TKey id)
    {
        if (_modifierTokens.TryGetValue(id, out var token))
        {
            token.Target.ClearModification(token);
            _modifierTokens.Remove(id);
        }

        return this;
    }

    public AttributeSetModifier<TKey> Clear()
    {
        foreach(var token in _modifierTokens.Values)
        {
            token.Target.ClearModification(token);
        }
        _modifierTokens.Clear();
        
        return this;
    }
}

public static class AttributeSetExtensions
{
    public static bool TryModify<TKey, T>(this AttributeSetModifier<TKey> setModifier, TKey id, T value)
    {
        if (setModifier.AttributeSet.IsAttributeModifiable<T>(id))
        {
            setModifier.Modify(id, value);
            return true;
        }

        return false;
    }
}