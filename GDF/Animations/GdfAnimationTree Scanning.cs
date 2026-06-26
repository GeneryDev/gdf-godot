using System.Collections.Generic;
using Godot;

namespace GDF.Animations;

public partial class GdfAnimationTree
{
    private ulong _lastScannedTreeRootId = 0;

    private void EnsureScanStillValid()
    {
        var currentRoot = TreeRoot;
        ulong currentInstanceId = currentRoot?.GetInstanceId() ?? 0;
        if (currentInstanceId != _lastScannedTreeRootId)
        {
            _lastScannedTreeRootId = currentInstanceId;
            ScanTree(currentRoot);
        }
    }

    private void ScanTree(AnimationRootNode root)
    {
        _animNodePathsToMetadataNodes.Clear();
        _eventListeners.Clear();
        _stateMachinePlaybacks.Clear();
        
        var allNodes = new List<(string Path, AnimationNode Node)>();
        allNodes.Add((null, root));
        CollectSubNodes(null, root, allNodes, recursive: true);

        foreach (var entry in allNodes)
        {
            ScanNode(entry.Path, entry.Node);
        }
    }

    private void ScanNode(string path, AnimationNode node)
    {
        // GD.Print($"Currently at {path} [{node.GetType().Name}]");

        if (node.IsClass(nameof(AnimationNodeStateMachine)))
        {
            ScanTransitions(path, node);
        }

        if (node.IsClass(nameof(AnimationNodeStateMachine)))
        {
            var playbackParamPath = $"parameters/{path}/playback";
            _stateMachinePlaybacks[playbackParamPath] = Get(playbackParamPath).As<AnimationNodeStateMachinePlayback>();
        }

        if (GetMetadata<GdfAnimationNodeMetadata>(node) is {} meta)
        {
            if (!string.IsNullOrEmpty(meta.Expression))
            {
                string propName = meta.GetTargetPropertyName(node);
                if (propName != null)
                {
                    var fullParamPath = $"parameters/{path}/{propName}";
                    // GD.Print($"Mapping {fullParamPath} to expression: {expr}");
                    _animNodePathsToMetadataNodes[fullParamPath] = (node, meta);
                    if (meta.HasExpression()) meta.GetParsedExpression(); // pre-parse expressions on scan
                }
            }

            string nodeName = path[(path.LastIndexOf('/') + 1)..];
            string parentPath = nodeName.Length == path.Length ? "" : path[..(path.Length - nodeName.Length - 1)];
            
            if (meta.TriggeringEvents is {Count: > 0})
            {
                foreach ((string evt, var mode) in meta.TriggeringEvents)
                {
                    var instance = new AnimationEventListener(mode, parentPath, nodeName);
                    if (!_eventListeners.ContainsKey(evt)) _eventListeners[evt] = new List<AnimationEventListener>();
                    _eventListeners[evt].Add(instance);
                }
            }
        }
    }

    private void ScanTransitions(string path, GodotObject sm)
    {
        int transitionCount = sm.Call(AnimationNodeStateMachine.MethodName.GetTransitionCount).AsInt32();
        for (var index = 0; index < transitionCount; index++)
        {
            var fromName = sm.Call(AnimationNodeStateMachine.MethodName.GetTransitionFrom, index).AsStringName();
            var toName = sm.Call(AnimationNodeStateMachine.MethodName.GetTransitionTo, index).AsStringName();
            var transition = sm.Call(AnimationNodeStateMachine.MethodName.GetTransition, index).As<AnimationNodeStateMachineTransition>();

            if (GetMetadata<GdfAnimationTransitionMetadata>(transition) is {} meta)
            {
                if (meta.TriggeringEvents is { Length: > 0 })
                {
                    foreach (string evt in meta.TriggeringEvents)
                    {
                        var instance = new AnimationEventListener(GdfAnimationEventAction.StateTravel, path, fromName, toName);
                        if (!_eventListeners.ContainsKey(evt))
                            _eventListeners[evt] = new List<AnimationEventListener>();
                        _eventListeners[evt].Add(instance);
                    }
                }
            } 
        }
    }

    private static void CollectSubNodes(string pathSoFar, AnimationNode parent, List<(string Path, AnimationNode Node)> output, bool recursive = false)
    {
        string propertyPrefix = parent.IsClass(nameof(AnimationNodeStateMachine))
            ? "states/"
            : parent.IsClass(nameof(AnimationNodeBlendTree))
                ? "nodes/"
                : parent.IsClass(nameof(AnimationNodeBlendSpace1D))
                    ? "blend_point_"
                    : null;
        if (propertyPrefix != null)
        {
            foreach (var property in parent.GetPropertyList())
            {
                string propName = property["name"].AsString();
                if (propName.StartsWith(propertyPrefix) && propName.EndsWith("/node"))
                {
                    string nodeName = propName.Substring(propertyPrefix.Length, propName.Length - (propertyPrefix.Length + "/node".Length));
                    var node = parent.GetSubNode(nodeName, fullPropertyName: propName);
                    if (node == null) continue;
                    string subPath = (pathSoFar != null ? pathSoFar + "/" : "") + nodeName;
                    output.Add((subPath, node));
                    if (recursive)
                        CollectSubNodes(subPath, node, output, true);
                }
            }
        }
    }

    private static T GetMetadata<[MustBeVariant]T>(GodotObject obj)
    {
        if (!obj.HasMeta(MetaNameMetadata)) return default;
        return obj.GetMeta(MetaNameMetadata).As<T>();
    }

    public static readonly StringName MetaNameMetadata = "gdf_metadata";
}