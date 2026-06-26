using System;
using System.Collections.Generic;
using Godot;

namespace GDF.Animations;

public partial class GdfAnimationTree
{
    private readonly Dictionary<string, List<AnimationEventListener>> _eventListeners = new();

    /// <summary>
    /// Retrieves a list of animation nodes that are listening to the given animation event name. 
    /// </summary>
    public List<AnimationNode> CollectListeningNodes(string evt, List<AnimationNode> output)
    {
        EnsureScanStillValid();
        output ??= new();
        if (_eventListeners.TryGetValue(evt, out var listeners))
        {
            foreach (var listener in listeners)
            {
                output.Add(listener.GetNode(this));
            }
        }
        return output;
    }
    
    public void TriggerEvent(string evt)
    {
        EnsureScanStillValid();
        if (string.IsNullOrEmpty(evt)) return;
        if (_eventListeners.TryGetValue(evt, out var listeners))
        {
            foreach (var listener in listeners)
            {
                listener.TriggerEvent(evt, this);
            }
        }
    }

    private readonly struct AnimationEventListener
    {
        private readonly GdfAnimationEventAction _action;
        private readonly string _parentPath;

        private readonly StringName _toNodeName;
        private readonly StringName _fromNodeName;

        public AnimationEventListener(GdfAnimationEventAction action, string parentPath, StringName toNodeName)
        {
            _action = action;
            _parentPath = parentPath;
            _toNodeName = toNodeName;
            _fromNodeName = null;
        }

        public AnimationEventListener(GdfAnimationEventAction action, string parentPath,
            StringName fromNodeName, StringName toNodeName)
        {
            _action = action;
            _parentPath = parentPath;
            _fromNodeName = fromNodeName;
            _toNodeName = toNodeName;
        }

        public AnimationNode GetNode(AnimationTree tree)
        {
            AnimationNode node = tree.TreeRoot;
            if (!string.IsNullOrEmpty(_parentPath))
            {
                foreach (string part in _parentPath.Split('/'))
                {
                    node = node.GetSubNode(part);
                }
            }

            return node.GetSubNode(_toNodeName);
        }

        private AnimationNodeStateMachinePlayback GetPlayback(AnimationTree tree)
        {
            var rawPlayback = tree.Get(
                string.IsNullOrEmpty(_parentPath)
                    ? "parameters/playback"
                    : $"parameters/{_parentPath}/playback"
            );
            return rawPlayback.As<AnimationNodeStateMachinePlayback>();
        }

        public void TriggerEvent(string evt, AnimationTree tree)
        {
            var playback = GetPlayback(tree);

            // GD.Print($"[{tree.Owner.Name}] [{Engine.GetPhysicsFrames()}] Triggering event {evt}; current node: {playback.GetCurrentNode()}; requiring: {_fromNodeName}");
            if (_fromNodeName != null)
            {
                if (playback?.GetCurrentNode() != _fromNodeName) return;
            }

            switch (_action)
            {
                case GdfAnimationEventAction.None:
                    break;
                case GdfAnimationEventAction.StateStart:
                    playback?.Start(_toNodeName, true);
                    tree.Advance(0);
                    break;
                case GdfAnimationEventAction.StateTravel:
                    playback?.Travel(_toNodeName, true);
                    tree.Advance(0);
                    break;
                case GdfAnimationEventAction.StateRestart:
                    if (playback != null)
                    {
                        if (playback.GetCurrentNode() == _toNodeName)
                        {
                            // restart
                            playback.Start(_toNodeName, true);
                            tree.Advance(0);
                        }
                    }

                    break;
                case GdfAnimationEventAction.StateTravelOrRestart:
                    if (playback != null)
                    {
                        if (playback.GetCurrentNode() == _toNodeName)
                        {
                            // restart
                            playback.Start(_toNodeName, true);
                        }
                        else
                        {
                            playback.Travel(_toNodeName, true);
                        }
                    }

                    tree.Advance(0);
                    break;
                case GdfAnimationEventAction.OneShotRequestFire:
                case GdfAnimationEventAction.OneShotRequestAbort:
                case GdfAnimationEventAction.OneShotRequestFadeOut:
                    tree.Set(
                        string.IsNullOrEmpty(_parentPath)
                            ? $"parameters/{_toNodeName}/request"
                            : $"parameters/{_parentPath}/{_toNodeName}/request",
                        Variant.From(
                            _action switch
                            {
                                GdfAnimationEventAction.OneShotRequestFire => AnimationNodeOneShot.OneShotRequest.Fire,
                                GdfAnimationEventAction.OneShotRequestAbort => AnimationNodeOneShot.OneShotRequest.Abort,
                                GdfAnimationEventAction.OneShotRequestFadeOut => AnimationNodeOneShot.OneShotRequest.FadeOut,
                                _ => AnimationNodeOneShot.OneShotRequest.Fire
                            }
                            )
                    );
                    tree.Advance(0);
                    break;
                default:
                {
                    GD.PrintErr($"Unrecognized animation event action: {_action}");
                    break;
                }
            }
        }
    }
}