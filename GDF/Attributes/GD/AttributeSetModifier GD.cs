using Godot;

namespace GDF.Attributes;

public partial class AttributeSetModifier<TKey>
{
    private Tween _fadeTween;

    public AttributeSetModifier<TKey> FadeIn(double seconds, float to = 1.0f,
        Tween.TransitionType transition = Tween.TransitionType.Linear, Tween.EaseType ease = Tween.EaseType.InOut)
    {
        SetWeight(0.0f);
        _fadeTween?.Kill();
        _fadeTween = CreateFadeTween(Weight, to, seconds, transition, ease, removeAtEnd: false);
        return this;
    }

    public AttributeSetModifier<TKey> FadeTo(float to, double seconds, Tween.TransitionType transition = Tween.TransitionType.Linear, Tween.EaseType ease = Tween.EaseType.InOut)
    {
        _fadeTween?.Kill();
        _fadeTween = CreateFadeTween(Weight, to, seconds, transition, ease, removeAtEnd: false);
        return this;
    }

    public AttributeSetModifier<TKey> FadeOutThenRemove(double seconds, Tween.TransitionType transition = Tween.TransitionType.Linear, Tween.EaseType ease = Tween.EaseType.InOut)
    {
        this.BindToNode(null);
        _fadeTween?.Kill();
        _fadeTween = CreateFadeTween(Weight, 0.0f, seconds, transition, ease, removeAtEnd: true);
        return null;
    }

    private Tween CreateFadeTween(float from, float to, double seconds,
        Tween.TransitionType transition = Tween.TransitionType.Linear, Tween.EaseType ease = Tween.EaseType.InOut,
        bool removeAtEnd = false)
    {
        var node = AttributeSet as Node;
        if (removeAtEnd && (node == null || !node.IsInsideTree()))
        {
            Clear();
            return null;
        }
        var tween = new Tween();
        if (node != null)
            tween.BindNode(node);
        

        tween.SetIgnoreTimeScale();
        tween.SetTrans(transition);
        tween.SetEase(ease);
        tween.SetProcessMode(Tween.TweenProcessMode.Idle);
        tween.TweenMethod(Callable.From((float weight) => SetWeight(weight)), from, to, seconds);
        if (removeAtEnd) tween.TweenCallback(Callable.From(Clear));
        return tween;
    }
}