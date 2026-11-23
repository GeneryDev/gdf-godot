using System;
using System.Collections.Generic;

namespace GDF.Attributes;

public abstract class ModifierChain<TParam> : IModifierChain, IModifiable<TParam>, IModifierComputable<ModifierChain<TParam>.Step>
{
    private readonly List<Step> _steps = new();
    
    private int _nextTokenId = 1;
    private int _editCount;

    ModifierToken IModifierChain.CreateToken(string description, float order)
    {
        int newTokenId = _nextTokenId++;
        var token = new ModifierToken(this, newTokenId, description, order);
        return token;
    }

    void IModifiable<TParam>.Modify(ModifierToken token, TParam param)
    {
        if(!AssertTokenOwn(token)) return;
        int existingIndex = StepIndexOfToken(token);
        var mod = existingIndex >= 0
            ? _steps[existingIndex]
            : new Step()
            {
                Token = token,
                Param = param,
                Weight = 1.0f
            };
        mod.Param = param;

        if (existingIndex >= 0)
        {
            _steps[existingIndex] = mod;
        }
        else
        {
            int insertionIndex = _steps.Count;
            while (insertionIndex > 0 && _steps[insertionIndex-1].Token.Order > token.Order) insertionIndex--;
        
            _steps.Insert(insertionIndex, mod);
        }

        MarkDirty();
    }

    public bool ClearModification(ModifierToken token)
    {
        int existingIndex = StepIndexOfToken(token);
        if (existingIndex < 0) return false;
        _steps.RemoveAt(existingIndex);
        MarkDirty();
        return true;
    }

    public void SetWeight(ModifierToken token, float weight)
    {
        int existingIndex = StepIndexOfToken(token);
        if (existingIndex < 0)
        {
            GdfLogger.Error("Cannot set weight for modifier token with no active modifications in chain!");
            return;
        }
        var mod = _steps[existingIndex];
        mod.Weight = weight;
        _steps[existingIndex] = mod;
        MarkDirty();
    }

    public void SetIntegrityChecker(ModifierToken token, IntegrityChecker integrityChecker)
    {
        int existingIndex = StepIndexOfToken(token);
        if (existingIndex < 0)
        {
            GdfLogger.Error("Cannot set integrity checker for modifier token with no active modifications in chain!");
            return;
        }
        var mod = _steps[existingIndex];
        mod.IntegrityChecker = integrityChecker;
        _steps[existingIndex] = mod;
    }

    public void CheckIntegrity(IntegrityCheckParams param)
    {
        for (var i = 0; i < _steps.Count; i++)
        {
            var modification = _steps[i];
            if (modification.IntegrityChecker == null) continue;
            
            int editCountBeforeCheck = _editCount;
            var result = modification.IntegrityChecker.Invoke(ref param);
            if (result.Failed)
            {
                if (param.RemoveFailures)
                {
                    GdfLogger.Error($"Failed integrity check for chain modifier token '{modification.Token.Description}': {result.ResultMessage}. Removing.");
                    if (_editCount == editCountBeforeCheck)
                    {
                        _steps.RemoveAt(i);
                        i--;
                        MarkDirty();
                    }
                    else
                    {
                        // The chain was changed externally during the integrity check,
                        // likely by the integrity checker itself.
                        // Restart the checks from the beginning.
                        i = -1;
                        continue;
                    }
                }
                else
                {
                    GdfLogger.Error($"Failed integrity check for chain modifier token '{modification.Token.Description}': {result.ResultMessage}. Removing.");
                }
            }
        }
    }

    public Step Compute(in Step initial)
    {
        var current = initial;
        // for is faster than foreach here 
        int count = _steps.Count;
        for (var i = 0; i < count; i++)
        {
            var mod = _steps[i];
            Reduce(ref current, mod, mod.Weight);
        }

        return current;
    }

    protected void MarkDirty()
    {
        _editCount++;
    }

    public int GetEditCount()
    {
        return _editCount;
    }

    public bool HasModification(ModifierToken token)
    {
        return StepIndexOfToken(token) != -1;
    }
    
    protected int StepIndexOfToken(ModifierToken token)
    {
        if(!AssertTokenOwn(token)) return -1;
        for (var i = 0; i < _steps.Count; i++)
        {
            if (_steps[i].Token == token) return i;
        }

        return -1;
    }

    public bool AssertTokenOwn(ModifierToken token)
    {
        if (token.Target != this)
        {
            GdfLogger.Error("Modifier Chain method called using a modifier token not belonging to it!");
            return false;
        }

        return true;
    }

    protected abstract void Reduce(ref Step current, in Step incoming, float weight);

    public struct Step
    {
        public TParam Param;
        public ModifierToken Token;
        public float Weight;
        public IntegrityChecker IntegrityChecker;
    }
}

public struct ModifierToken : IEquatable<ModifierToken>
{
    public readonly IModifierChain Target;
    public readonly int Id;
    public string Description;
    public float Order;

    public bool IsEmpty => Id == 0;

    public ModifierToken(IModifierChain target, int id, string description, float order)
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

    public bool Equals(ModifierToken other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object obj)
    {
        return obj is ModifierToken other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Id;
    }

    public static bool operator ==(ModifierToken left, ModifierToken right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ModifierToken left, ModifierToken right)
    {
        return !left.Equals(right);
    }
}

public struct ModifierTokenTyped<TChain> where TChain : IModifierChain
{
    public readonly TChain Target;
    public ModifierToken Token;
    public string Description => Token.Description;
    public float Order => Token.Order;

    public ModifierTokenTyped(TChain target, ModifierToken token)
    {
        Target = target;
        Token = token;
    }

    public static implicit operator ModifierToken(ModifierTokenTyped<TChain> typed)
    {
        return typed.Token;
    }

    public ModifierTokenTyped<TChain> SetWeight(float weight)
    {
        Target.SetWeight(Token, weight);
        return this;
    }
}

public static class ModifierChainExtensions
{
    // CreateToken
    
    public static ModifierTokenTyped<TChain> CreateToken<TChain>(this TChain chain, string description, float order = 0)
        where TChain : IModifierChain
    {
        var token = chain.CreateToken(description, order);
        return new ModifierTokenTyped<TChain>(chain, token);
    }
    
    public static ModifierTokenTyped<TChain> CreateToken<TAttr, TChain>(this TAttr attr, string description, float order = 0)
        where TAttr : IAttribute<TChain>
        where TChain : IModifierChain
    {
        var chain = attr.GetModifierChain();
        var token = chain.CreateToken(description, order);
        return new ModifierTokenTyped<TChain>(chain, token);
    }

    
    // Modify
    
    public static TChain Modify<TChain, TParam>(this TChain chain, ModifierToken token, TParam param)
        where TChain : IModifierChain, IModifiable<TParam>
    {
        chain.Modify(token, param);
        return chain;
    }

    public static ModifierTokenTyped<TChain> Modify<TChain, TParam>(this ModifierTokenTyped<TChain> token, TParam param)
        where TChain : IModifierChain, IModifiable<TParam>
    {
        token.Target.Modify(token, param);
        return token;
    }
    
    public static TAttr Modify<TAttr, TChain, TParam>(this TAttr attr, ModifierToken token, TParam param)
        where TAttr : IAttribute<TChain>
        where TChain : IModifierChain, IModifiable<TParam>
    {
        attr.GetModifierChain().Modify(token, param);
        return attr;
    }

    // TryModify
    
    public static bool TryModify<TChain, TParam>(this TChain chain, ModifierToken token, TParam param)
        where TChain : IModifierChain
    {
        if (chain is IModifiable<TParam> modifiable)
        {
            modifiable.Modify(token, param);
            return true;
        }

        return false;
    }

    public static bool TryModify<TChain, TParam>(this ModifierTokenTyped<TChain> token, TParam param)
        where TChain : IModifierChain
    {
        return token.Target.TryModify(token, param);
    }
    
    public static bool TryModify<TAttr, TChain, TParam>(this TAttr attr, ModifierToken token, TParam param)
        where TAttr : IAttribute<TChain>
        where TChain : IModifierChain, IModifiable<TParam>
    {
        return attr.GetModifierChain().TryModify(token, param);
    }
    
    // Compute
    public static TValue Compute<TChain, TValue>(this TChain chain, TValue initial)
        where TChain : IModifierChain, IModifierComputable<TValue>
    {
        return chain.Compute(initial);
    }
}