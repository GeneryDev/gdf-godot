using System;
using GDF.Editor;
using GDF.Util;
using Godot;
using Godot.Collections;

namespace GDF.Tools;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/tool_wrench.png")]
public partial class AnimationTimingTool : Node
{
    [Export] public string OriginalControlPoints;
    [Export] public string CurrentControlPoints;
    [Export] public string DesiredControlPoints;

#if TOOLS
    [ExportToolButton("Reset")] private Callable ButtonReset => new Callable(this, MethodName.Reset);
    [ExportToolButton("Original Control Points Done")] private Callable ButtonOriginalControlPointsDone => new Callable(this, MethodName.OriginalControlPointsDone);
    [ExportToolButton("Update Timing")] private Callable ButtonUpdateTiming => new Callable(this, MethodName.UpdateTiming);
    
    private static EditorUndoRedoManager UndoRedo => EditorInterface.Singleton.GetEditorUndoRedo();

    private Transactions _transactions;
    private EditorUndoRedoManager _undoRedo;

    private void GetAnimation(out AnimationMixer mixer, out Animation anim)
    {
        mixer = AnimationEditorUtil.GetEditingAnimationMixer();
        anim = null;
        string animName = AnimationEditorUtil.GetAnimationPlayerEditor()?.CurrentAnimationName;
        if (mixer != null && animName != null) anim = mixer?.GetAnimation(animName);
    }

    private bool FindTrackIndices(Animation anim, Node animRootNode, out int trackIndexA, out int trackIndexB,
        out int trackIndexC)
    {
        trackIndexA = trackIndexB = trackIndexC = -1;
        for (var trackIdx = 0; trackIdx < anim.GetTrackCount(); trackIdx++)
        {
            var track = anim.GetTrack(trackIdx);
            var path = track.Path;
            var animatedNode = animRootNode.GetNode(path);
            if (animatedNode != this) continue;
            string propName = path.GetSubName(path.GetSubNameCount() - 1);

            switch (propName)
            {
                case nameof(OriginalControlPoints):
                    trackIndexA = trackIdx;
                    break;
                case nameof(CurrentControlPoints):
                    trackIndexB = trackIdx;
                    break;
                case nameof(DesiredControlPoints):
                    trackIndexC = trackIdx;
                    break;
            }
        }

        return !(trackIndexA == -1 || trackIndexB == -1 || trackIndexC == -1);
    }

    public void Reset()
    {
        GetAnimation(out var mixer, out var anim);
        if (anim == null) return;
        _transactions = new Transactions(UndoRedo, anim, "Animation Timing Tool: Reset");

        var animRootNode = mixer.GetNode(mixer.RootNode);
        FindTrackIndices(anim, animRootNode,
            out int trackIndexA,
            out int trackIndexB,
            out int trackIndexC
        );

        var pathToThisNode = animRootNode.GetPathTo(this);

        // Add any missing tracks
        _transactions.InitializeTrack(nameof(OriginalControlPoints), anim, trackIndexA, pathToThisNode,
            new TrackState { Enabled = true });
        _transactions.InitializeTrack(nameof(DesiredControlPoints), anim, trackIndexC, pathToThisNode,
            new TrackState { Enabled = false });
        _transactions.InitializeTrack(nameof(CurrentControlPoints), anim, trackIndexB, pathToThisNode,
            new TrackState { Enabled = false, Imported = true });

        // Find tracks again
        FindTrackIndices(anim, animRootNode,
            out trackIndexA,
            out trackIndexB,
            out trackIndexC
        );
        _transactions.InsertKey(trackIndexA, new KeyState { Time = 0, Value = "Start" });
        _transactions.InsertKey(trackIndexA, new KeyState { Time = anim.Length, Value = "End" });
    }

    public void OriginalControlPointsDone()
    {
        GetAnimation(out var mixer, out var anim);
        if (anim == null) return;
        _transactions = new Transactions(UndoRedo, anim, "Animation Timing Tool: Original Control Points Done");

        var animRootNode = mixer.GetNode(mixer.RootNode);
        if (!FindTrackIndices(anim, animRootNode,
                out int trackIndexA,
                out int trackIndexB,
                out int trackIndexC
            ))
        {
            GD.PrintErr("One or more control point tracks not found in current animation");
            return;
        }

        int controlPointCount = anim.TrackGetKeyCount(trackIndexA);
        if (controlPointCount < 2)
        {
            GD.PrintErr(
                $"Not enough control points in the '{nameof(OriginalControlPoints)}' track! At least 2 control points are needed");
            return;
        }

        if (anim.TrackGetKeyCount(trackIndexB) > 0)
        {
            GD.PrintErr($"The '{nameof(CurrentControlPoints)}' track must be empty!");
            return;
        }

        if (anim.TrackGetKeyCount(trackIndexC) > 0)
        {
            GD.PrintErr($"The '{nameof(DesiredControlPoints)}' track must be empty!");
            return;
        }

        // Copy all control point keyframes from the original to both "current" and "desired" control point tracks
        for (var keyIndex = 0; keyIndex < controlPointCount; keyIndex++)
        {
            var state = KeyState.CreateFrom(anim, trackIndexA, keyIndex);

            _transactions.InsertKey(trackIndexB, state);
            _transactions.InsertKey(trackIndexC, state);
        }

        // Update checkboxes
        _transactions.UpdateTrack(trackIndexA, new TrackState() { Enabled = false });
        _transactions.UpdateTrack(trackIndexB, new TrackState() { Enabled = false, Imported = true });
        _transactions.UpdateTrack(trackIndexC, new TrackState() { Enabled = true });
    }


    public void UpdateTiming()
    {
        GetAnimation(out var mixer, out var anim);
        if (anim == null) return;
        UpdateTiming(anim, mixer, true);
    }

    public void UpdateTiming(Animation anim, AnimationMixer mixer, bool errorIfMissingTracks = false)
    {
        if (anim == null) return;
        _transactions = new Transactions(UndoRedo, anim, "Animation Timing Tool: Update Animation Timing");

        var animRootNode = mixer.GetNode(mixer.RootNode);
        FindTrackIndices(anim, animRootNode,
            out int trackIndexA,
            out int trackIndexB,
            out int trackIndexC
        );

        if (trackIndexA == -1 || trackIndexC == -1)
        {
            if (errorIfMissingTracks)
                GD.PrintErr(
                    $"One or more control point tracks not found in current animation. In animation: '{anim.ResourceName}'");
            return;
        }

        int controlPointCount = anim.TrackGetKeyCount(trackIndexA);
        var reimported = false;

        if (controlPointCount < 2)
        {
            GD.PrintErr(
                $"Not enough control points in the '{nameof(OriginalControlPoints)}' track! At least 2 control points are needed. In animation: '{anim.ResourceName}'");
            return;
        }

        if (trackIndexB == -1)
        {
            RecoverCurrentControlPointTrack(anim, animRootNode);
            reimported = true;

            // Find tracks again
            FindTrackIndices(anim, animRootNode,
                out trackIndexA,
                out trackIndexB,
                out trackIndexC
            );
        }

        if (anim.TrackGetKeyCount(trackIndexB) != controlPointCount)
        {
            GD.PrintErr(
                $"The '{nameof(CurrentControlPoints)}' track must have exactly {controlPointCount} keyframes! If you need a different number of control points, press Reset and redesign the original control point track. In animation: '{anim.ResourceName}'");
            return;
        }

        if (anim.TrackGetKeyCount(trackIndexC) != controlPointCount)
        {
            GD.PrintErr(
                $"The '{nameof(DesiredControlPoints)}' track must have exactly {controlPointCount} keyframes! If you need a different number of control points, press Reset and redesign the original control point track. In animation: '{anim.ResourceName}'");
            return;
        }

        var oldTimes = new double[controlPointCount];
        var newTimes = new double[controlPointCount];

        var changed = false;

        // Copy all control point keyframes from the original to both "current" and "desired" control point tracks
        for (var keyIndex = 0; keyIndex < controlPointCount; keyIndex++)
        {
            double oldTime = oldTimes[keyIndex] = anim.TrackGetKeyTime(trackIndexB, keyIndex);
            double newTime = newTimes[keyIndex] = anim.TrackGetKeyTime(trackIndexC, keyIndex);

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (oldTime != newTime) changed = true;
        }

        if (!changed)
            // GD.Print("No timing changes");
            return;

        for (var trackIndex = 0; trackIndex < anim.GetTrackCount(); trackIndex++)
        {
            if (trackIndex == trackIndexA || trackIndex == trackIndexC)
                continue; // Skip over Original Control Points track and Desired Control Points track
            if (reimported && !anim.TrackIsImported(trackIndex))
                continue; // If the animation was re-imported, only re-time tracks that were imported.
            _transactions.RetimeTrack(trackIndex, oldTimes, newTimes);
        }

        _transactions.RetimeLength(oldTimes, newTimes);

        // Update checkboxes
        _transactions.UpdateTrack(trackIndexA, new TrackState() { Enabled = false });
        _transactions.UpdateTrack(trackIndexB, new TrackState() { Enabled = false, Imported = true });
        _transactions.UpdateTrack(trackIndexC, new TrackState() { Enabled = true });
        // GD.Print("Timing changes!");
    }

    private void RecoverCurrentControlPointTrack(Animation anim, Node animRootNode)
    {
        FindTrackIndices(anim, animRootNode,
            out int trackIndexA,
            out int trackIndexB,
            out int trackIndexC
        );

        var pathToThisNode = animRootNode.GetPathTo(this);
        _transactions.InitializeTrack(nameof(CurrentControlPoints), anim, trackIndexB, pathToThisNode,
            new TrackState { Enabled = false, Imported = true });

        FindTrackIndices(anim, animRootNode,
            out trackIndexA,
            out trackIndexB,
            out trackIndexC
        );

        int controlPointCount = anim.TrackGetKeyCount(trackIndexA);

        // Copy all control point keyframes from the original to the "current" track
        for (var keyIndex = 0; keyIndex < controlPointCount; keyIndex++)
        {
            var state = KeyState.CreateFrom(anim, trackIndexA, keyIndex);

            _transactions.InsertKey(trackIndexB, state);
        }

        FindTrackIndices(anim, animRootNode,
            out trackIndexA,
            out trackIndexB,
            out trackIndexC
        );
        // _transactions.MoveTrack(trackIndexB, trackIndexA+1);
    }

    public float? GetOriginalRetimedAnimationLength(Animation anim, AnimationMixer mixer)
    {
        if (anim == null) return null;

        var animRootNode = mixer.GetNode(mixer.RootNode);
        FindTrackIndices(anim, animRootNode,
            out int trackIndexA,
            out _,
            out _
        );
        if (trackIndexA == -1) return null;

        int controlPointCount = anim.TrackGetKeyCount(trackIndexA);

        if (controlPointCount < 2) return null;

        return (float)anim.TrackGetKeyTime(trackIndexA, controlPointCount - 1);
    }


    public readonly struct Transactions
    {
        private readonly EditorUndoRedoManager _undoRedo;
        private readonly Animation _anim;
        private readonly string _actionName;

        public Transactions(EditorUndoRedoManager undoRedo, Animation anim, string actionName)
        {
            _undoRedo = undoRedo;
            _anim = anim;
            _actionName = actionName;
        }

        public void CreateAction(bool backwardUndoOps = false)
        {
            _undoRedo.CreateAction(_actionName ?? "Animation Timing Tool", Godot.UndoRedo.MergeMode.All, _anim,
                backwardUndoOps);
        }

        public int InitializeTrack(string propertyName, Animation anim, int index, NodePath pathToThisNode,
            TrackState state)
        {
            if (index < 0)
            {
                // add track
                int newIndex = anim.GetTrackCount();
                AddTrack(Animation.TrackType.Value, newIndex, pathToThisNode + ":" + propertyName);
                index = newIndex;
            }

            UpdateTrack(index, state);
            ClearTrack(index);

            return index;
        }

        public void MoveTrack(int fromIndex, int toIndex)
        {
            CreateAction(true);
            _undoRedo.AddDoMethod(_anim, Animation.MethodName.TrackMoveTo, fromIndex, toIndex);
            _undoRedo.AddUndoMethod(_anim, Animation.MethodName.TrackMoveTo, toIndex, fromIndex);
            _undoRedo.CommitAction();
        }

        public void AddTrack(Animation.TrackType type, int atIndex, NodePath path)
        {
            CreateAction(true);
            _undoRedo.AddDoMethod(_anim, Animation.MethodName.AddTrack, Variant.From(type), atIndex);
            _undoRedo.AddDoMethod(_anim, Animation.MethodName.TrackSetPath, atIndex, path);
            _undoRedo.AddUndoMethod(_anim, Animation.MethodName.RemoveTrack, atIndex);
            _undoRedo.CommitAction();
        }

        public void UpdateTrack(int trackIndex, TrackState state)
        {
            CreateAction(true);
            state.Write(_undoRedo, _anim, trackIndex);
            _undoRedo.CommitAction();
        }

        public void ClearTrack(int trackIndex)
        {
            CreateAction(true);
            for (int keyIdx = _anim.TrackGetKeyCount(trackIndex) - 1; keyIdx >= 0; keyIdx--)
                KeyTransaction.WriteRemoval(_undoRedo, _anim, trackIndex, keyIdx);
            _undoRedo.CommitAction();
        }

        public void RemoveTrack(int trackIndex)
        {
            var trackPath = _anim.TrackGetPath(trackIndex);
            var trackType = _anim.TrackGetType(trackIndex);
            var trackState = TrackState.CreateFrom(_anim, trackIndex);

            ClearTrack(trackIndex);

            // Remove
            CreateAction(true);
            _undoRedo.AddDoMethod(_anim, Animation.MethodName.RemoveTrack, trackIndex);
            trackState.WriteUndo(_undoRedo, _anim, trackIndex);
            _undoRedo.AddUndoMethod(_anim, Animation.MethodName.TrackSetPath, trackIndex, trackPath);
            _undoRedo.AddUndoMethod(_anim, Animation.MethodName.AddTrack, Variant.From(trackType), trackIndex);
            _undoRedo.CommitAction();
        }

        public void InsertKey(int trackIndex, KeyState state)
        {
            CreateAction(true);
            KeyTransaction.WriteInsertion(_undoRedo, _anim, trackIndex, state);
            _undoRedo.CommitAction();
        }

        public void CopyTrackKeys(int trackIndex, Animation sourceAnimation, int sourceTrackIndex)
        {
            CreateAction(true);
            int keyCount = sourceAnimation.TrackGetKeyCount(sourceTrackIndex);
            for (var keyIndex = 0; keyIndex < keyCount; keyIndex++)
            {
                var keyState = KeyState.CreateFrom(sourceAnimation, sourceTrackIndex, keyIndex);
                KeyTransaction.WriteInsertion(_undoRedo, _anim, trackIndex, keyState);
            }

            _undoRedo.CommitAction();
        }

        public void RetimeLength(double[] oldTimes, double[] newTimes)
        {
            double oldLength = _anim.Length;
            double newLength = RemapTime(oldLength, oldTimes, newTimes);
            CreateAction(true);
            _undoRedo.AddDoProperty(_anim, Animation.PropertyName.Length, newLength);
            _undoRedo.AddUndoProperty(_anim, Animation.PropertyName.Length, oldLength);
            _undoRedo.CommitAction();
        }

        public void RetimeTrack(int trackIndex, double[] oldTimes, double[] newTimes)
        {
            // Godot doesn't allow manipulating method keyframes any way other than moving them
            bool moveOnly = _anim.TrackGetType(trackIndex) == Animation.TrackType.Method;

            CreateAction(true);
            int trackKeyCount = _anim.TrackGetKeyCount(trackIndex);
            var keyTransactions = new KeyTransaction[trackKeyCount];

            // Move to a temporary time, past the last keyframe
            // This is needed to avoid keyframes "merging" as we're moving them around one by one
            for (int keyIndex = trackKeyCount - 1; keyIndex >= 0; keyIndex--)
            {
                var transaction = KeyTransaction.CreateFrom(_anim, trackIndex, keyIndex);
                keyTransactions[keyIndex] = transaction;

                double originalTime = transaction.State.Time;

                if (moveOnly)
                {
                    // Move the keyframe to a temporary time after the end of the animation, keeping the same order
                    double tempTime = _anim.Length + 10 + keyIndex;

                    _undoRedo.AddDoMethod(transaction, KeyTransaction.MethodName.Move, tempTime);
                    _undoRedo.AddUndoMethod(transaction, KeyTransaction.MethodName.Move, originalTime);
                }
                else
                {
                    // remove the keyframe instead
                    _undoRedo.AddDoMethod(transaction, KeyTransaction.MethodName.Remove);
                    _undoRedo.AddUndoMethod(transaction, KeyTransaction.MethodName.Insert, originalTime);
                }
            }

            for (var keyIndex = 0; keyIndex < trackKeyCount; keyIndex++)
            {
                var transaction = keyTransactions[keyIndex];

                double originalTime = transaction.State.Time;
                double remappedTime = RemapTime(originalTime, oldTimes, newTimes);

                if (moveOnly)
                {
                    // Move the keyframe back from the temporary time to the new remapped time
                    double tempTime = _anim.Length + 10 + keyIndex;
                    _undoRedo.AddDoMethod(transaction, KeyTransaction.MethodName.Move, remappedTime);
                    _undoRedo.AddUndoMethod(transaction, KeyTransaction.MethodName.Move, tempTime);
                }
                else
                {
                    // re-insert the keyframe instead
                    _undoRedo.AddDoMethod(transaction, KeyTransaction.MethodName.Insert, remappedTime);
                    _undoRedo.AddUndoMethod(transaction, KeyTransaction.MethodName.Remove);
                }
            }

            _undoRedo.CommitAction();
        }

        private double RemapTime(double time, double[] oldTimes, double[] newTimes)
        {
            if (time <= oldTimes[0]) return newTimes[0] - (oldTimes[0] - time);
            if (time >= oldTimes[^1]) return newTimes[^1] + (time - oldTimes[^1]);
            for (var i = 0; i < oldTimes.Length - 1; i++)
                if (time <= oldTimes[i + 1])
                {
                    double oldFrom = oldTimes[i];
                    double oldTo = oldTimes[i + 1];
                    double newFrom = newTimes[i];
                    double newTo = newTimes[i + 1];
                    return Mathf.Remap(time, oldFrom, oldTo, newFrom, newTo);
                }

            throw new Exception("Should have been impossible to get here");
        }
    }

    public override void _ValidateProperty(Dictionary property)
    {
        base._ValidateProperty(property);
        var propertyName = property["name"].AsStringName();
        var usage = property["usage"].As<PropertyUsageFlags>();
        if (propertyName == PropertyName.OriginalControlPoints ||
            propertyName == PropertyName.CurrentControlPoints ||
            propertyName == PropertyName.DesiredControlPoints)
            usage &= ~PropertyUsageFlags.Storage;
        property["usage"] = Variant.From(usage);
    }
}

public partial class KeyTransaction : GodotObject
{
    public KeyState State;

    private Animation _anim;
    private int _trackIndex;
    private int _keyIndex;

    public static KeyTransaction CreateFrom(Animation anim, int trackIndex, int keyIndex)
    {
        var state = KeyState.CreateFrom(anim, trackIndex, keyIndex);
        var transaction = new KeyTransaction()
        {
            _anim = anim,
            _trackIndex = trackIndex,
            _keyIndex = keyIndex,
            State = state
        };
        return transaction;
    }

    public void Insert(double time)
    {
        State.Time = time;
        Insert();
    }

    public void Insert()
    {
        _keyIndex = State.InsertTo(_anim, _trackIndex);
    }

    public void Remove()
    {
        _anim.TrackRemoveKey(_trackIndex, _keyIndex);
        _keyIndex = -1;
    }

    public void Move(double time)
    {
        int keyIndex = _anim.TrackFindKey(_trackIndex, State.Time, Animation.FindMode.Exact);
        if (keyIndex == -1)
        {
            double? closestTime = null;
            for (var i = 0; i < _anim.TrackGetKeyCount(_trackIndex); i++)
            {
                double keyTime = _anim.TrackGetKeyTime(_trackIndex, i);
                if (!closestTime.HasValue || Math.Abs(keyTime - time) < Math.Abs(closestTime.Value - time))
                    closestTime = keyTime;
            }

            throw new Exception(
                $"Failed to move key in track {_anim.TrackGetPath(_trackIndex)} at time {State.Time} to time {time}; couldn't find the keyframe! Closest was at {closestTime}");
        }

        _anim.TrackSetKeyTime(_trackIndex, keyIndex, time);
        _keyIndex = _anim.TrackFindKey(_trackIndex, State.Time, Animation.FindMode.Exact);
        State.Time = time;
    }

    public static void WriteMove(EditorUndoRedoManager undoRedo, Animation anim, int trackIndex, int keyIndex,
        double toTime)
    {
        var transaction = CreateFrom(anim, trackIndex, keyIndex);
        double oldTime = transaction.State.Time;

        undoRedo.AddDoMethod(transaction, MethodName.Move, toTime);
        undoRedo.AddUndoMethod(transaction, MethodName.Move, oldTime);
    }

    public static void WriteRemoval(EditorUndoRedoManager undoRedo, Animation anim, int trackIndex, int keyIndex)
    {
        var transaction = CreateFrom(anim, trackIndex, keyIndex);

        undoRedo.AddDoMethod(transaction, MethodName.Remove);
        undoRedo.AddUndoMethod(transaction, MethodName.Insert);
    }

    public static void WriteInsertion(EditorUndoRedoManager undoRedo, Animation anim, int trackIndex, KeyState state)
    {
        var transaction = new KeyTransaction()
        {
            _anim = anim,
            _trackIndex = trackIndex,
            _keyIndex = -1,
            State = state
        };

        undoRedo.AddDoMethod(transaction, MethodName.Insert);
        undoRedo.AddUndoMethod(transaction, MethodName.Remove);
    }
}

public partial class KeyState : GodotObject
{
    public double Time;
    public Variant Value;
    public float Transition;

    public StringName Animation;

    public float BezierValue;
    public Vector2 BezierInHandle;
    public Vector2 BezierOutHandle;

    public Resource AudioStream;
    public float AudioStartOffset;
    public float AudioEndOffset;

    public StringName Method;
    public Godot.Collections.Array MethodParams;

    public static KeyState CreateFrom(Animation anim, int trackIndex, int keyIndex)
    {
        var state = new KeyState()
        {
            Time = anim.TrackGetKeyTime(trackIndex, keyIndex),
            Value = anim.TrackGetKeyValue(trackIndex, keyIndex),
            Transition = anim.TrackGetKeyTransition(trackIndex, keyIndex)
        };
        switch (anim.TrackGetType(trackIndex))
        {
            case Godot.Animation.TrackType.Method:
            {
                // This crashes Godot, and the results can't be written to a keyframe anyway, so skip them.
                // state.Method = anim.MethodTrackGetName(trackIndex, keyIndex);
                // state.MethodParams = anim.MethodTrackGetParams(trackIndex, keyIndex);
                break;
            }
            case Godot.Animation.TrackType.Bezier:
            {
                state.BezierValue = anim.BezierTrackGetKeyValue(trackIndex, keyIndex);
                state.BezierInHandle = anim.BezierTrackGetKeyInHandle(trackIndex, keyIndex);
                state.BezierOutHandle = anim.BezierTrackGetKeyOutHandle(trackIndex, keyIndex);
                break;
            }
            case Godot.Animation.TrackType.Audio:
            {
                state.AudioStream = anim.AudioTrackGetKeyStream(trackIndex, keyIndex);
                state.AudioStartOffset = anim.AudioTrackGetKeyStartOffset(trackIndex, keyIndex);
                state.AudioEndOffset = anim.AudioTrackGetKeyEndOffset(trackIndex, keyIndex);
                break;
            }
            case Godot.Animation.TrackType.Animation:
            {
                state.Animation = anim.AnimationTrackGetKeyAnimation(trackIndex, keyIndex);
                break;
            }
            default: break;
        }

        return state;
    }

    public int InsertTo(Animation anim, int trackIndex)
    {
        return anim.TrackGetType(trackIndex) switch
        {
            // Godot.Animation.TrackType.Method
            //     => anim.AnimationTrackInsertKey(trackIndex, Time, Animation),
            Godot.Animation.TrackType.Bezier
                => anim.BezierTrackInsertKey(trackIndex, Time, BezierValue, BezierInHandle, BezierOutHandle),
            Godot.Animation.TrackType.Audio
                => anim.AudioTrackInsertKey(trackIndex, Time, AudioStream, AudioStartOffset, AudioEndOffset),
            Godot.Animation.TrackType.Animation
                => anim.AnimationTrackInsertKey(trackIndex, Time, Animation),
            _ => anim.TrackInsertKey(trackIndex, Time, Value, Transition)
        };
    }
}

public partial class TrackState : GodotObject
{
    public Animation.TrackType TrackType = Animation.TrackType.Value;

    public bool Enabled = true;
    public bool Imported = false;
    public Animation.UpdateMode UpdateMode = Animation.UpdateMode.Discrete;
    public Animation.InterpolationType InterpolationType = Animation.InterpolationType.Nearest;
    public bool InterpolationLoopWrap = false;

    public static TrackState CreateFrom(Animation anim, int trackIndex)
    {
        var trackType = anim.TrackGetType(trackIndex);
        return new TrackState()
        {
            TrackType = trackType,
            Enabled = anim.TrackIsEnabled(trackIndex),
            Imported = anim.TrackIsImported(trackIndex),
            UpdateMode = trackType == Animation.TrackType.Value ? anim.ValueTrackGetUpdateMode(trackIndex) : default,
            InterpolationType = anim.TrackGetInterpolationType(trackIndex),
            InterpolationLoopWrap = anim.TrackGetInterpolationLoopWrap(trackIndex)
        };
    }

    public void WriteDo(EditorUndoRedoManager undoRedo, Animation anim, int trackIndex)
    {
        undoRedo.AddDoMethod(anim, Animation.MethodName.TrackSetEnabled, trackIndex, Enabled);
        undoRedo.AddDoMethod(anim, Animation.MethodName.TrackSetImported, trackIndex, Imported);
        if (TrackType == Animation.TrackType.Value)
            undoRedo.AddDoMethod(anim, Animation.MethodName.ValueTrackSetUpdateMode, trackIndex,
                Variant.From(UpdateMode));
        undoRedo.AddDoMethod(anim, Animation.MethodName.TrackSetInterpolationType, trackIndex,
            Variant.From(InterpolationType));
        undoRedo.AddDoMethod(anim, Animation.MethodName.TrackSetInterpolationLoopWrap, trackIndex,
            InterpolationLoopWrap);
    }

    public void WriteUndo(EditorUndoRedoManager undoRedo, Animation anim, int trackIndex)
    {
        undoRedo.AddUndoMethod(anim, Animation.MethodName.TrackSetEnabled, trackIndex, Enabled);
        undoRedo.AddUndoMethod(anim, Animation.MethodName.TrackSetImported, trackIndex, Imported);
        if (TrackType == Animation.TrackType.Value)
            undoRedo.AddUndoMethod(anim, Animation.MethodName.ValueTrackSetUpdateMode, trackIndex,
                Variant.From(UpdateMode));
        undoRedo.AddUndoMethod(anim, Animation.MethodName.TrackSetInterpolationType, trackIndex,
            Variant.From(InterpolationType));
        undoRedo.AddUndoMethod(anim, Animation.MethodName.TrackSetInterpolationLoopWrap, trackIndex,
            InterpolationLoopWrap);
    }

    public void Write(EditorUndoRedoManager undoRedo, Animation anim, int trackIndex)
    {
        var prevState = CreateFrom(anim, trackIndex);

        WriteDo(undoRedo, anim, trackIndex);
        prevState.WriteUndo(undoRedo, anim, trackIndex);
    }
#endif
}