using System.Text;
using GDF.Util;
using Godot;
using Godot.Collections;

namespace GDF.Animations;

[Tool]
[GlobalClass]
public partial class GdfAnimationNodeMetadata : Resource
{
    [Export] public string AnimationNodeClassName = "";
    [ExportGroup("Auto-Set via Expression")]
    [Export(PropertyHint.Expression)]
    public string Expression = "";
    [Export]
    public float BlendSpeed = -1;
    [ExportGroup("Trigger via Events")]
    [Export]
    public Dictionary<string, GdfAnimationEventAction> TriggeringEvents = new();
    
    private Expression _cachedExpression;

    public string GetTargetPropertyName(AnimationNode node)
    {
        switch (node.GetClass())
        {
            case "AnimationNodeOneShot": return "request";
            case "AnimationNodeAdd2": return "add_amount";
            case "AnimationNodeAdd3": return "add_amount";
            case "AnimationNodeBlend2": return "blend_amount";
            case "AnimationNodeBlend3": return "blend_amount";
            case "AnimationNodeBlendSpace1D": return "blend_position";
            case "AnimationNodeBlendSpace2D": return "blend_position";
            case "AnimationNodeStateMachine": return "playback";
            case "AnimationNodeSub2": return "sub_amount";
            case "AnimationNodeTimeScale": return "scale";
            case "AnimationNodeTimeSeek": return "seek_request";
            case "AnimationNodeTransition": return "transition_request";
        }
        GD.PrintErr($"Unsupported class for GDF Animation Metadata: {node.GetClass()}");
        return null;
    }

    public static bool Supports(GodotObject obj)
    {
        return obj?.IsClass("AnimationNode") ?? false;
    }

    public Expression GetParsedExpression()
    {
        return _cachedExpression ??= ExpressionUtil.Parse(Expression);
    }

    public bool HasExpression()
    {
        return !string.IsNullOrEmpty(Expression);
    }

    public Variant Blend(Variant from, Variant to, double delta)
    {
        var blendT = (float)(delta * BlendSpeed);
        return from.VariantType switch
        {
            Variant.Type.Float or Variant.Type.Int => ExpDecay.LerpOverTime(from.AsSingle(), to.AsSingle(), blendT),
            Variant.Type.Vector2 => ExpDecay.LerpOverTime(from.AsVector2(), to.AsVector2(), blendT),
            Variant.Type.Vector3 => ExpDecay.LerpOverTime(from.AsVector3(), to.AsVector3(), blendT),
            Variant.Type.Color => ExpDecay.LerpOverTime(from.AsColor(), to.AsColor(), blendT),
            Variant.Type.Quaternion => ExpDecay.SlerpOverTime(from.AsQuaternion(), to.AsQuaternion(), blendT),
            _ => to
        };
    }


    public override void _ValidateProperty(Dictionary property)
    {
        var propName = property["name"].AsStringName();
        var usage = property["usage"].As<PropertyUsageFlags>();

        if (propName == PropertyName.AnimationNodeClassName)
        {
            usage &= ~PropertyUsageFlags.Editor;
        }

#if TOOLS
        if (propName == PropertyName.TriggeringEvents)
        {
            property["hint_string"] = $"4/0:;2/2:{GdfAnimationEventActions.GetAnimationEventActionOptionsString(AnimationNodeClassName)}";
        }
#endif

        property["usage"] = Variant.From(usage);
    }
}