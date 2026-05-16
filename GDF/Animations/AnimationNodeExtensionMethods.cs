using Godot;

namespace GDF.Animations;

public static class AnimationNodeExtensionMethods
{
    public static AnimationNode GetSubNode(this AnimationNode node, StringName name, StringName fullPropertyName = null)
    {
        if (node is AnimationNodeBlendTree bt) return bt.GetNode(name);
        if (node is AnimationNodeStateMachine sm) return sm.GetNode(name);
        if (node.IsClass(nameof(AnimationNodeBlendTree)) || node.IsClass(nameof(AnimationNodeStateMachine)))
        {
            return node.Call(AnimationNodeBlendTree.MethodName.GetNode, name).As<AnimationNode>();
        }
        if (node.IsClass(nameof(AnimationNodeBlendSpace1D)) || node.IsClass(nameof(AnimationNodeBlendSpace2D)))
        {
            return node.Get(fullPropertyName ?? ("blend_point_" + name + "/node")).As<AnimationNode>();
        }

        return null;
    }
}