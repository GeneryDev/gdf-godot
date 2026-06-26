using Godot;

namespace GDF.Animations;

public readonly struct AnimationTreeNavigator
{
    private readonly AnimationTree _tree;
    private readonly string _parentPath;

    private readonly StringName _toNodeName;
    private readonly StringName _fromNodeName;

    public AnimationTreeNavigator(AnimationTree tree, string parentPath, StringName toNodeName)
    {
        _tree = tree;
        _parentPath = parentPath;
        _toNodeName = toNodeName;
        _fromNodeName = null;
    }

    public AnimationTreeNavigator(AnimationTree tree, string parentPath, StringName fromNodeName, StringName toNodeName)
    {
        _tree = tree;
        _parentPath = parentPath;
        _fromNodeName = fromNodeName;
        _toNodeName = toNodeName;
    }

    public StringName GetNodeName()
    {
        if (IsStateTransition()) return null;
        return _toNodeName;
    }

    public bool IsStateTransition()
    {
        return _fromNodeName != null;
    }

    public StringName StateTransitionGetFromNodeName()
    {
        if (!IsStateTransition()) return null;
        return _fromNodeName;
    }

    public StringName StateTransitionGetToNodeName()
    {
        if (!IsStateTransition()) return null;
        return _toNodeName;
    }

    public string GetParentPath()
    {
        return _parentPath;
    }

    public string GetPath()
    {
        if (IsStateTransition()) return null;
        return _parentPath + '/' + _toNodeName;
    }

    public AnimationTreeNavigator? GetParent()
    {
        if (string.IsNullOrEmpty(_parentPath)) return null;
        int lastSlashIndex = _parentPath.LastIndexOf('/');
        if (lastSlashIndex == -1)
        {
            return new AnimationTreeNavigator(_tree, null, _parentPath);
        }
        else
        {
            string grandParentPath = _parentPath[..lastSlashIndex];
            string parentName = _parentPath[(lastSlashIndex + 1)..];
            return new AnimationTreeNavigator(_tree, grandParentPath, parentName);
        }
    }

    public AnimationTreeNavigator? StateTransitionGetFrom()
    {
        if (!IsStateTransition()) return null;
        return GetParent()?.GetChild(_fromNodeName);
    }

    public AnimationTreeNavigator? StateTransitionGetTo()
    {
        if (!IsStateTransition()) return null;
        return GetParent()?.GetChild(_toNodeName);
    }

    public AnimationTreeNavigator? GetChild(StringName subNodeName)
    {
        return new AnimationTreeNavigator(_tree, GetPath(), subNodeName);
    }

    public AnimationNode GetNode()
    {
        if (IsStateTransition()) return null;
        return GetParentNode()?.GetSubNode(_toNodeName);
    }

    public AnimationNode GetParentNode()
    {
        if (GetParent() is { } parent) return parent.GetNode();
        else return _tree.TreeRoot;
    }

    public AnimationNodeStateMachinePlayback GetStateMachinePlayback()
    {
        if (IsStateTransition()) return null;
        var rawPlayback = _tree.Get(
            string.IsNullOrEmpty(_parentPath)
                ? $"parameters/{_toNodeName}/playback"
                : $"parameters/{_parentPath}/{_toNodeName}/playback"
        );
        return rawPlayback.As<AnimationNodeStateMachinePlayback>();
    }
}