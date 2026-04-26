using System.Collections.Generic;
using GDF.PropertyStacks.Definitions;
using GDF.PropertyStacks.Internal;
using Godot;

namespace GDF.PropertyStacks;

public class PropertyFrame
{
    public readonly PropertyFrameHandle Handle;
    public readonly PropertyStack Stack;

    private readonly HashSet<string> _propertyIdsSet;
    private float _weight = 1.0f;
    
    private Tween _fadeTween;

    public float Weight
    {
        get => _weight;
        set => SetWeight(value);
    }

    public PropertyFrame(PropertyFrameHandle handle, PropertyStack stack)
    {
        Handle = handle;
        Stack = stack;
        _propertyIdsSet = new HashSet<string>();
    }

    public PropertyFrame Set<T>(string id, T value)
    {
        Stack.SetFrameProperty(Handle, id, value);
        Stack.SetFramePropertyWeight(Handle, id, _weight);
        _propertyIdsSet.Add(id);
        return this;
    }

    public PropertyFrame Set<T>(string id, ModificationOperation op, T value)
    {
        return Set(id, new VectorModification<T>()
        {
            Operation = op,
            Value = value
        });
    }

    public PropertyFrame SetWeight(float weight)
    {
        _weight = weight;
        foreach (string propertyId in _propertyIdsSet)
        {
            Stack.SetFramePropertyWeight(Handle, propertyId, weight);
        }

        return this;
    }

    public PropertyFrame BindToNode(Node node)
    {
        ulong? instanceId = node?.GetInstanceId();

        Stack.SetFrameValidator(Handle, new PropertyStack.FrameValidator()
        {
            BoundNodeId = instanceId
        });
        return this;
    }

    public PropertyFrame Remove()
    {
        _fadeTween?.Kill();
        Stack.RemoveFrame(Handle);
        return null;
    }
    

    public PropertyFrame FadeIn(double seconds, float to = 1.0f, Tween.TransitionType transition = Tween.TransitionType.Linear, Tween.EaseType ease = Tween.EaseType.InOut)
    {
        SetWeight(0.0f);
        _fadeTween?.Kill();
        _fadeTween = Stack.CreateFrameTween(this, Weight, to, seconds, transition, ease, removeAtEnd: false);
        return this;
    }

    public PropertyFrame FadeTo(float to, double seconds, Tween.TransitionType transition = Tween.TransitionType.Linear, Tween.EaseType ease = Tween.EaseType.InOut)
    {
        _fadeTween?.Kill();
        _fadeTween = Stack.CreateFrameTween(this, Weight, to, seconds, transition, ease, removeAtEnd: false);
        return this;
    }

    public PropertyFrame FadeOutThenRemove(double seconds, Tween.TransitionType transition = Tween.TransitionType.Linear, Tween.EaseType ease = Tween.EaseType.InOut)
    {
        BindToNode(null);
        _fadeTween?.Kill();
        if (!Stack.IsInsideTree()) return Remove();
        _fadeTween = Stack.CreateFrameTween(this, Weight, 0.0f, seconds, transition, ease, removeAtEnd: true);
        return null;
    }

    public bool HasProperty(string propertyId)
    {
        return _propertyIdsSet.Contains(propertyId);
    }

    public int GetIndex()
    {
        return Stack.GetFrameIndex(Handle);
    }
}