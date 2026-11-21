using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace GDF.Attributes;

public abstract class Attribute<TValue, TParam>: IAttribute, IAttributeModifiable<TParam>, IAttributeComputable<TValue>
{
    public abstract TValue CurrentValue { get; }

    private readonly List<Modification> _modifications = new();
    private Modification _computedModification;
    
    private int _nextTokenId = 1;
    private int _editCount;
    private bool _computedDirty = true;

    AttributeModifierToken IAttribute.CreateToken(string description, float order)
    {
        int newTokenId = _nextTokenId++;
        var token = new AttributeModifierToken(this, newTokenId, description, order);
        return token;
    }

    void IAttributeModifiable<TParam>.Modify(AttributeModifierToken token, TParam param)
    {
        if(!AssertTokenOwn(token)) return;
        int existingIndex = ModIndexOfToken(token);
        var mod = existingIndex >= 0
            ? _modifications[existingIndex]
            : new Modification()
            {
                Token = token,
                Param = param,
                Weight = 1.0f
            };
        mod.Param = param;

        if (existingIndex >= 0)
        {
            _modifications[existingIndex] = mod;
        }
        else
        {
            int insertionIndex = _modifications.Count;
            while (insertionIndex > 0 && _modifications[insertionIndex-1].Token.Order > token.Order) insertionIndex--;
        
            _modifications.Insert(insertionIndex, mod);
        }

        MarkDirty();
    }

    bool IAttribute.ClearModification(AttributeModifierToken token)
    {
        int existingIndex = ModIndexOfToken(token);
        if (existingIndex < 0) return false;
        _modifications.RemoveAt(existingIndex);
        MarkDirty();
        return true;
    }

    void IAttribute.SetWeight(AttributeModifierToken token, float weight)
    {
        int existingIndex = ModIndexOfToken(token);
        if (existingIndex < 0)
        {
            GdfLogger.Error("Cannot set weight for modifier token with no active modifications in attribute!");
            return;
        }
        var mod = _modifications[existingIndex];
        mod.Weight = weight;
        _modifications[existingIndex] = mod;
        MarkDirty();
    }

    public void SetIntegrityChecker(AttributeModifierToken token, IntegrityChecker integrityChecker)
    {
        int existingIndex = ModIndexOfToken(token);
        if (existingIndex < 0)
        {
            GdfLogger.Error("Cannot set integrity checker for modifier token with no active modifications in attribute!");
            return;
        }
        var mod = _modifications[existingIndex];
        mod.IntegrityChecker = integrityChecker;
        _modifications[existingIndex] = mod;
    }

    public void CheckIntegrity(IntegrityCheckParams param)
    {
        for (var index = 0; index < _modifications.Count; index++)
        {
            var modification = _modifications[index];
            if (modification.IntegrityChecker == null) continue;
            
            int editCountBeforeCheck = _editCount;
            var result = modification.IntegrityChecker.Invoke(ref param);
            if (result.Failed)
            {
                if (param.RemoveFailures)
                {
                    GdfLogger.Error($"Failed integrity check for attribute modifier token '{modification.Token.Description}': {result.ResultMessage}. Removing.");
                    if (_editCount == editCountBeforeCheck)
                    {
                        _modifications.RemoveAt(index);
                        index--;
                        MarkDirty();
                    }
                    else
                    {
                        // The attribute was changed externally during the integrity check,
                        // likely by the integrity checker itself.
                        // Restart the checks from the beginning.
                        index = -1;
                        continue;
                    }
                }
                else
                {
                    GdfLogger.Error($"Failed integrity check for attribute modifier token '{modification.Token.Description}': {result.ResultMessage}. Removing.");
                }
            }
        }
    }

    public TParam Recompute()
    {
        var current = new Modification()
        {
            Param = GetInitialParam(),
            Weight = 1.0f
        };
        foreach (var mod in _modifications)
        {
            current = Reduce(current, mod, mod.Weight);
        }

        _computedModification = current;
        _computedDirty = false;
        return _computedModification.Param;
    }

    protected abstract TParam GetInitialParam();

    protected Modification GetComputed()
    {
        if (_computedDirty) Recompute();
        return _computedModification;
    }

    protected void MarkDirty()
    {
        _editCount++;
        _computedDirty = true;
    }

    int IAttribute.GetEditCount()
    {
        return _editCount;
    }

    bool IAttribute.HasModification(AttributeModifierToken token)
    {
        return ModIndexOfToken(token) != -1;
    }
    
    protected int ModIndexOfToken(AttributeModifierToken token)
    {
        if(!AssertTokenOwn(token)) return -1;
        for (var i = 0; i < _modifications.Count; i++)
        {
            if (_modifications[i].Token == token) return i;
        }

        return -1;
    }

    protected bool AssertTokenOwn(AttributeModifierToken token)
    {
        if (token.Target != this)
        {
            GdfLogger.Error("Attribute method called using a modifier token not belonging to it!");
            return false;
        }

        return true;
    }

    protected abstract Modification Reduce(in Modification current, in Modification incoming, float weight);

    protected struct Modification
    {
        public TParam Param;
        public AttributeModifierToken Token;
        public float Weight;
        public IntegrityChecker IntegrityChecker;
    }
}

public struct AttributeModifierToken : IEquatable<AttributeModifierToken>
{
    public readonly IAttribute Target;
    public readonly int Id;
    public string Description;
    public float Order;

    public bool IsEmpty => Id == 0;

    public AttributeModifierToken(IAttribute target, int id, string description, float order)
    {
        this.Target = target;
        this.Id = id;
        Description = description;
        Order = order;
    }

    public readonly void SetWeight(float weight)
    {
        Target.SetWeight(this, weight);
    }

    public readonly void SetIntegrityChecker(IntegrityChecker integrityChecker)
    {
        Target.SetIntegrityChecker(this, integrityChecker);
    }

    public bool Equals(AttributeModifierToken other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object obj)
    {
        return obj is AttributeModifierToken other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Id;
    }

    public static bool operator ==(AttributeModifierToken left, AttributeModifierToken right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(AttributeModifierToken left, AttributeModifierToken right)
    {
        return !left.Equals(right);
    }
}

public struct AttributeModifierTokenTyped<TAttr> where TAttr : IAttribute
{
    public readonly TAttr Target;
    public AttributeModifierToken Token;
    public string Description => Token.Description;
    public float Order => Token.Order;

    public AttributeModifierTokenTyped(TAttr target, AttributeModifierToken token)
    {
        Target = target;
        Token = token;
    }

    public static implicit operator AttributeModifierToken(AttributeModifierTokenTyped<TAttr> typed)
    {
        return typed.Token;
    }

    public AttributeModifierTokenTyped<TAttr> SetWeight(float weight)
    {
        Target.SetWeight(Token, weight);
        return this;
    }
}

public static class AttributeExtensions
{
    public static AttributeModifierTokenTyped<TAttr> CreateToken<TAttr>(this TAttr attr, string description, float order = 0) where TAttr : IAttribute
    {
        var token = attr.CreateToken(description, order);
        return new AttributeModifierTokenTyped<TAttr>(attr, token);
    }

    public static TAttr Modify<TAttr, TParam>(this TAttr attr, AttributeModifierToken token, TParam param)
        where TAttr : IAttribute, IAttributeModifiable<TParam>
    {
        attr.Modify(token, param);
        return attr;
    }

    public static AttributeModifierTokenTyped<TAttr> Modify<TAttr, TParam>(this AttributeModifierTokenTyped<TAttr> token, TParam param)
        where TAttr : IAttribute, IAttributeModifiable<TParam>
    {
        token.Target.Modify(token, param);
        return token;
    }

    public static bool TryModify<TAttr, TParam>(this TAttr attr, AttributeModifierToken token, TParam param)
        where TAttr : IAttribute
    {
        if (attr is IAttributeModifiable<TParam> modifiable)
        {
            modifiable.Modify(token, param);
            return true;
        }

        return false;
    }

    public static bool TryModify<TAttr, TParam>(this AttributeModifierTokenTyped<TAttr> token, TParam param)
        where TAttr : IAttribute
    {
        return token.Target.TryModify(token, param);
    }
}

[StructLayout(LayoutKind.Explicit)]
public struct Empty {}
