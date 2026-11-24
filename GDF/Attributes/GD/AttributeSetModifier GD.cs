using Godot;

namespace GDF.Attributes;

public partial class BulkModifier
{
    private Tween _fadeTween;
    private Node _boundNode;
    
    public BulkModifier BindToNode(Node node)
    {
        _boundNode = node;
        if (node != null)
        {
            var nodeName = node.Name;
            SetIntegrityChecker((ref IntegrityCheckParams param) =>
            {
                if (GodotObject.IsInstanceValid(node))
                {
                    return new IntegrityCheckResult()
                    {
                        Passed = true
                    };
                }
                else
                {
                    if (param.RemoveFailures)
                        Clear();
                    return new IntegrityCheckResult()
                    {
                        Passed = false,
                        ResultMessage = $"Attribute set modifier is now stale due to bound node (formerly '{nodeName}') having been freed."
                    };
                }
            });
        }
        else
        {
            SetIntegrityChecker(null);
        }
        return this;
    }

    public BulkModifier FadeIn(double seconds, float to = 1.0f,
        Tween.TransitionType transition = Tween.TransitionType.Linear, Tween.EaseType ease = Tween.EaseType.InOut)
    {
        SetWeight(0.0f);
        _fadeTween?.Kill();
        _fadeTween = CreateFadeTween(Weight, to, seconds, transition, ease, removeAtEnd: false);
        return this;
    }

    public BulkModifier FadeTo(float to, double seconds, Tween.TransitionType transition = Tween.TransitionType.Linear, Tween.EaseType ease = Tween.EaseType.InOut)
    {
        _fadeTween?.Kill();
        _fadeTween = CreateFadeTween(Weight, to, seconds, transition, ease, removeAtEnd: false);
        return this;
    }

    public BulkModifier FadeOutThenDiscard(double seconds, Tween.TransitionType transition = Tween.TransitionType.Linear, Tween.EaseType ease = Tween.EaseType.InOut)
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
        var node = _boundNode;
        if (removeAtEnd && (node == null || !node.IsInsideTree()))
        {
            BindToNode(null);
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