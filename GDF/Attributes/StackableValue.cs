using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Godot;

namespace GDF.Attributes;

public abstract class StackableValue<TValue, TParam>: IStackableValue<TValue>
{
    public abstract TValue CurrentValue { get; }

    private readonly List<Modification> _modifications = new();
    private Modification _computedModification;
    
    private int _nextTokenId = 1;
    private int _editCount;
    private bool _computedDirty = true;
    
    
    public StackableValueTokenTyped<TValue, TParam> CreateToken(string description, float order = 0)
    {
        int newTokenId = _nextTokenId++;
        var token = new StackableValueToken(this, newTokenId, description, order);
        return new StackableValueTokenTyped<TValue, TParam>(this, token);
    }

    StackableValueToken IStackableValue.CreateToken(string description, float order)
    {
        return CreateToken(description, order).Token;
    }

    public void Modify(StackableValueToken token, TParam param)
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

        _computedDirty = true;
        _editCount++;
    }

    public bool ClearModification(StackableValueToken token)
    {
        int existingIndex = ModIndexOfToken(token);
        if (existingIndex < 0) return false;
        _modifications.RemoveAt(existingIndex);
        _computedDirty = true;
        _editCount++;
        return true;
    }

    public void SetWeight(StackableValueToken token, float weight)
    {
        int existingIndex = ModIndexOfToken(token);
        if (existingIndex < 0)
        {
            GD.PrintErr("Cannot set weight for token not set in stackable value!");
            return;
        }
        var mod = _modifications[existingIndex];
        mod.Weight = weight;
        _modifications[existingIndex] = mod;
        _computedDirty = true;
        _editCount++;
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

    public int GetEditCount()
    {
        return _editCount;
    }

    public bool TokenHasValue(StackableValueToken token)
    {
        return ModIndexOfToken(token) != -1;
    }
    
    protected int ModIndexOfToken(StackableValueToken token)
    {
        if(!AssertTokenOwn(token)) return -1;
        for (var i = 0; i < _modifications.Count; i++)
        {
            if (_modifications[i].Token == token) return i;
        }

        return -1;
    }

    protected bool AssertTokenOwn(StackableValueToken token)
    {
        if (token.Target != this)
        {
            GD.PrintErr("Stackable value method called using a token not belonging to it!");
            return false;
        }

        return true;
    }

    protected abstract Modification Reduce(in Modification current, in Modification incoming, float weight);

    protected struct Modification
    {
        public TParam Param;
        public StackableValueToken Token;
        public float Weight;
    }
}

public interface IStackableValue
{
    public StackableValueToken CreateToken(string description, float order = 0);
    public bool ClearModification(StackableValueToken token);
}

public interface IStackableValue<out T> : IStackableValue
{
    public T CurrentValue { get; }
}

public interface IStackableValueSettable<in TIn, out TParam> : IStackableValueSettable<TIn>
{
    public TParam InToParam(TIn value);
}

public interface IStackableValueSettable<in T>
{
    public void Set(StackableValueToken token, T value);
}

public interface IStackableValueComputable<out T>
{
    public T Compute();
}

public struct StackableValueToken : IEquatable<StackableValueToken>
{
    public readonly IStackableValue Target;
    public readonly int Id;
    public string Description;
    public float Order;

    public bool IsEmpty => Id == 0;

    public StackableValueToken(IStackableValue target, int id, string description, float order)
    {
        this.Target = target;
        this.Id = id;
        Description = description;
        Order = order;
    }

    public bool Equals(StackableValueToken other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object obj)
    {
        return obj is StackableValueToken other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Id;
    }

    public static bool operator ==(StackableValueToken left, StackableValueToken right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(StackableValueToken left, StackableValueToken right)
    {
        return !left.Equals(right);
    }
}

public struct StackableValueTokenTyped<TValue, TParam>
{
    public readonly StackableValue<TValue, TParam> Target;
    public StackableValueToken Token;
    public string Description => Token.Description;
    public float Order => Token.Order;

    public StackableValueTokenTyped(StackableValue<TValue, TParam> target, StackableValueToken token)
    {
        Target = target;
        Token = token;
    }

    public static implicit operator StackableValueToken(StackableValueTokenTyped<TValue, TParam> typed)
    {
        return typed.Token;
    }

    public StackableValueTokenTyped<TValue, TParam> Modify(TParam param)
    {
        Target.Modify(Token, param);
        return this;
    }

    public StackableValueTokenTyped<TValue, TParam> SetWeight(float weight)
    {
        Target.SetWeight(Token, weight);
        return this;
    }
}

[StructLayout(LayoutKind.Explicit)]
public struct Empty {}