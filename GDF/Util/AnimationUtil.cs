using System;
using Godot;

namespace GDF.Util;

public static class AnimationUtil
{
    public static AnimationTrack GetTrack(this Animation anim, int id)
    {
        if (id < 0 || id >= anim.GetTrackCount())
        {
            throw new IndexOutOfRangeException();
        }
        return new AnimationTrack
        {
            Animation = anim,
            Id = id
        };
    }
}

public struct AnimationTrack
{
    public Animation Animation;
    public int Id;

    public bool IsImported
    {
        get => Animation.TrackIsImported(Id);
        set => Animation.TrackSetImported(Id, value);
    }

    public bool IsEnabled
    {
        get => Animation.TrackIsEnabled(Id);
        set => Animation.TrackSetEnabled(Id, value);
    }

    public bool IsCompressed => Animation.TrackIsCompressed(Id);
    public NodePath Path
    {
        get => Animation.TrackGetPath(Id);
        set => Animation.TrackSetPath(Id, value);
    }

    public Animation.TrackType Type => Animation.TrackGetType(Id);
    public Animation.InterpolationType InterpolationType
    {
        get => Animation.TrackGetInterpolationType(Id);
        set => Animation.TrackSetInterpolationType(Id, value);
    }

    public bool InterpolationLoopWrap
    {
        get => Animation.TrackGetInterpolationLoopWrap(Id);
        set => Animation.TrackSetInterpolationLoopWrap(Id, value);
    }

    public int KeyCount => Animation.TrackGetKeyCount(Id);

    // Warning: This does not modify other existing AnimationTrack structs, other than the one passed as a parameter
    public void Swap(AnimationTrack other)
    {
        Animation.TrackSwap(Id, other.Id);
        int prevThisId = Id;
        this.Id = other.Id;
        other.Id = prevThisId;
    }

    // Warning: This does not modify other existing AnimationTrack structs
    public void MoveTo(int toId)
    {
        Animation.TrackMoveTo(Id, toId);
        this.Id = toId;
    }

    public void MoveUp()
    {
        if (Id <= 0) return;
        Animation.TrackMoveUp(Id);
        Id--;
    }
    public void MoveDown()
    {
        if (Id >= Animation.GetTrackCount() - 1) return;
        Animation.TrackMoveDown(Id);
        Id++;
    }

    public int FindKey(double time, Animation.FindMode findMode = Animation.FindMode.Nearest)
    {
        return Animation.TrackFindKey(Id, time, findMode);
    }

    public int InsertKey(double time, Variant key, float transition = 1)
    {
        return Animation.TrackInsertKey(Id, time, key, transition);
    }

    public void InsertKey(int keyId)
    {
        Animation.TrackRemoveKey(Id, keyId);
    }

    public double GetKeyTime(int keyId)
    {
        return Animation.TrackGetKeyTime(Id, keyId);
    }

    public float GetKeyTransition(int keyId)
    {
        return Animation.TrackGetKeyTransition(Id, keyId);
    }

    public Variant GetKeyValue(int keyId)
    {
        return Animation.TrackGetKeyValue(Id, keyId);
    }

    public void SetKeyTime(int keyId, double time)
    {
        Animation.TrackSetKeyTime(Id, keyId, time);
    }

    public void SetKeyTransition(int keyId, float transition)
    {
        Animation.TrackSetKeyTransition(Id, keyId, transition);
    }

    public void SetKeyValue(int keyId, Variant value)
    {
        Animation.TrackSetKeyValue(Id, keyId, value);
    }

    public void RemoveKey(int keyId)
    {
        Animation.TrackRemoveKey(Id, keyId);
    }

    public void RemoveKeyAtTime(double time)
    {
        Animation.TrackRemoveKeyAtTime(Id, time);
    }
    
    // Missing bezier track
}