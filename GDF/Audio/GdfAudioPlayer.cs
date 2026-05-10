using System.Collections.Generic;
using System.Text;
using GDF.Util;
using Godot;
using Godot.Collections;

namespace GDF.Audio;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/audio_player.png")]
public partial class GdfAudioPlayer : Node
{
    public static readonly StringName GroupNameMuted = "muted";

    public const int ReleaseActionStop = 0;
    public const int ReleaseActionPlayToEnd = 1;
    public const int ReleaseActionFadeOut = 2;
    
    private static readonly List<GdfAudioPlayer> ActiveAudioPlayers = new();

    [Signal]
    public delegate void FinishedEventHandler();
    
    [Export]
    public TriggerModeEnum TriggerMode;

    [Export] public AnimationPlayer AnimationPlayer;
    
    [ExportToolButton("Find Animation Player")]
    private Callable ButtonFindAnimationPlayer => new(this, MethodName.FindAndSetAnimationPlayer);
    
    // [ExportToolButton("Test Animation Transformation")]
    // private Callable ButtonTestAnimationTransformation => new(this, MethodName.TestAnimationTransformation);

    [Export] public AudioStream Stream;

    [Export]
    public bool Playing
    {
        get => IsPlaying();
        set
        {
            if (Playing == value) return;
            if(value) play();
            else stop();
        }
    }

    [ExportGroup("Player Settings")]
    [Export(PropertyHint.Range,"-80,80,0.001,suffix:dB")]
    public float VolumeDb = 0;
    [Export(PropertyHint.Range,"0.01,4,0.01")]
    public float PitchScale = 1.0f;
    [Export(PropertyHint.Enum)]
    public StringName Bus = "Master";

    [ExportGroup("Audio Pooling")]
    [Export] public bool UseGlobalPool;
    [Export(PropertyHint.Enum,"Stop,Play to End,Fade Out")] public int ReleaseAction = ReleaseActionStop;
    [Export(PropertyHint.Range,"0,10,0.01,or_greater,suffix:s")] public float FadeOutTime = 1.0f;
    [Export(PropertyHint.Range,"1,16,1,or_greater")]
    public int MaxPolyphony = 1;

    [ExportGroup("De-Duplication")]
    [Export(PropertyHint.GroupEnable)] public bool UseDeduplication;

    [Export(PropertyHint.Range, "0,1,0.01,or_greater,suffix:s")]
    public float DeduplicationTimeFrameSec = 0.2f;
    
    [ExportGroup("3D")]
    [Export(PropertyHint.GroupEnable)] public bool Use3D;
    [Export] public bool TrackPosition = true;
    [Export] public AudioStreamPlayer3D.AttenuationModelEnum AttenuationModel = AudioStreamPlayer3D.AttenuationModelEnum.InverseDistance;
    [Export(PropertyHint.Range,"0.1,100,0.01,or_greater")]
    public float UnitSize = 10.0f;
    [Export(PropertyHint.Range,"-80,6,0.001,suffix:dB")]
    public float MaxDb = 3;
    [Export(PropertyHint.Range,"0,4096,0.01,or_greater,suffix:m")]
    public float MaxDistance = 0.0f;
    [Export(PropertyHint.Range,"0,3,0.01,or_greater")]
    public float PanningStrength = 1.0f;
    [Export(PropertyHint.Layers3DPhysics)]
    public uint AreaMask = 1;

    [ExportSubgroup("Attenuation Filter")]
    [Export(PropertyHint.Range, "1,20500,1,suffix:Hz")]
    public int AttenuationFilterCutoffHz = 5000;
    [Export(PropertyHint.Range,"-80,0,0.1,suffix:dB")]
    public int AttenuationFilterDb = -24;

    [ExportGroup("Animation")]
    [Export] public bool StopOnAnimationChanged = true;
        
    [ExportGroup("Parameters", "Parameter")]
    [Export] public Variant ParameterLooping;

    [ExportGroup("")]
    [ExportToolButton("Play")]
    private Callable ButtonPlay => new(this, MethodName.play);

    private Node _polyphonicPlayer;
    private List<Node> _claimedPlayers = new();
    private ulong _lastPlayedTime;
    private string _lastPlayedStreamPath;
    private bool _inActiveList = false;
    private bool _nextPlayPolyphonic = false;
    private bool _muted;

    private AudioPlayerPoolInstance _internalPool = new();

    public override void _Ready()
    {
        base._Ready();
        _muted = CheckMuted();
        if(TriggerMode == TriggerModeEnum.Autoplay && !Engine.IsEditorHint()) Play();
    }

    private bool CheckMuted()
    {
        if (GetTree().GetNodeCountInGroup(GroupNameMuted) == 0) return false;
        Node ancestor = this;
        while (ancestor != null)
        {
            if (ancestor.IsInGroup(GroupNameMuted)) return true;
            ancestor = ancestor.GetParent();
        }

        return false;
    }

    public void Trigger()
    {
        play();
    }

    public void Play()
    {
        play();
    }

    public void Stop()
    {
        stop();
    }

    // Intentionally named in snake_case.
    // Serves as a virtual method for animation mixers to call when inside an Audio Track.
    // ReSharper disable once InconsistentNaming
    public bool playing
    {
        get => Playing;
        set => Playing = value;
    }
    
    // Intentionally named in snake_case.
    // Serves as a virtual method for animation mixers to call when inside an Audio Track.
    // ReSharper disable once InconsistentNaming
    public AudioStream get_stream()
    {
        // TestPrint($"get_stream() {GetPolyphonicPlayer().GetStream()}");
        // Called exclusively by animations
        return GetPolyphonicPlayer().GetStream();
    }

    // Intentionally named in snake_case.
    // Serves as a virtual method for animation mixers to call when inside an Audio Track.
    // ReSharper disable once InconsistentNaming
    public void set_stream(AudioStream stream)
    {
        // ONLY called by animation players with polyphonic streams
        // TestPrint($"set_stream({stream})");
        
        if (stream is AudioStreamPolyphonic && !(TriggerMode == TriggerModeEnum.Animation && AnimationPlayer != null))
        {
            GD.PrintErr($"An animation player is playing audio clips to a {nameof(GdfAudioPlayer)} not set up with TriggerMode=Animation and an AnimationPlayer set.\n{this.GetSceneAndPathString()}");
            return;
        }

        if (stream == null)
        {
            // calling to stop
            if (ReleaseAction != ReleaseActionStop) return;
        }
        else
        {
            _nextPlayPolyphonic = true;
        }
        GetPolyphonicPlayer().SetStream(stream);
    }

    public bool IsPlaying()
    {
        // check if ANY are playing
        foreach (var playerNode in _claimedPlayers)
        {
            if (new AudioStreamPlayerRef(playerNode).IsPlaying()) return true;
        }

        return false;
    }

    // Intentionally named in snake_case.
    // Serves as a virtual method for animation mixers to call when inside an Audio Track.
    // ReSharper disable once InconsistentNaming
    public bool is_playing()
    {
        // TestPrint($"is_playing() {GetPolyphonicPlayer().IsPlaying()}");
        // Called exclusively by animations
        return GetPolyphonicPlayer().IsPlaying();
    }

    // Intentionally named in snake_case.
    // Serves as a virtual method for animation mixers to call when inside an Audio Track.
    // ReSharper disable once InconsistentNaming
    public void play()
    {
        // Called by animations and possibly by method calls
        play(0);
    }
    
    // Intentionally named in snake_case.
    // Serves as a virtual method for animation mixers to call when inside an Audio Track.
    // ReSharper disable once InconsistentNaming
    public void play(float fromPosition)
    {
        // Called by animations and possibly by method calls
        // TestPrint("play()");
        if (!IsInsideTree())
        {
            GD.PrintErr($"Cannot play audio from outside the tree: {this.GetSceneAndPathString()}");
            return;
        }
        if (_nextPlayPolyphonic)
        {
            GetPolyphonicPlayer().Play();
            _nextPlayPolyphonic = false;
            return;
        }
        if (_muted) return;

        if (UseDeduplication && FindDuplicatingAudioPlayer(out int matchingInstanceCount, out var earliestMatchingInstance))
        {
            // TestPrint("Deduplicated");
            return;
        }

        var player = ClaimAudioPlayer();
        player.SetStream(Stream);
        CopyProperties(player.Player);
        player.Play(fromPosition);

        _lastPlayedTime = Time.GetTicksMsec();
        _lastPlayedStreamPath = Stream?.ResourcePath;
        RemoveFromActiveList();
        AddToActiveList();
    }

    // Intentionally named in snake_case.
    // Serves as a virtual method for animation mixers to call when inside an Audio Track.
    // ReSharper disable once InconsistentNaming
    public void stop()
    {
        // Called by animations and possibly by method calls
        // TestPrint("stop()");
        ReleaseClaimedPlayers();

        if(_polyphonicPlayer != null && ReleaseAction is ReleaseActionStop) GetPolyphonicPlayer().Stop();
    }

    // Intentionally named in snake_case.
    // Serves as a virtual method for animation mixers to call when inside an Audio Track.
    // ReSharper disable once InconsistentNaming
    public bool has_stream_playback()
    {
        // Called exclusively by animations
        return GetPolyphonicPlayer().HasStreamPlayback();
    }

    // Intentionally named in snake_case.
    // Serves as a virtual method for animation mixers to call when inside an Audio Track.
    // ReSharper disable once InconsistentNaming
    public AudioStreamPlayback get_stream_playback()
    {
        // Called exclusively by animations
        return GetPolyphonicPlayer().GetStreamPlayback();
    }

    // Intentionally named in snake_case.
    // Serves as a virtual method for animation mixers to call when inside an Audio Track.
    // ReSharper disable once InconsistentNaming
    public bool get_is_sample()
    {
        // Called exclusively by animations
        return false;
    }

    // Intentionally named in snake_case.
    // Serves as a virtual method for animation mixers to call when inside an Audio Track.
    // ReSharper disable once InconsistentNaming
    public StringName get_bus()
    {
        // Called exclusively by animations
        return Bus;
    }

    // Intentionally named in snake_case.
    // Serves as a virtual method for animation mixers to call when inside an Audio Track.
    // ReSharper disable once InconsistentNaming
    public AudioServer.PlaybackType get_playback_type()
    {
        // Called exclusively by animations
        return AudioServer.PlaybackType.Default;
    }


    public void SwitchToClipByName(StringName clipName)
    {
        GD.Print($"Swap to audio clip '{clipName}'");
        
        foreach (var playerNode in _claimedPlayers)
        {
            if (new AudioStreamPlayerRef(playerNode).GetStreamPlayback() is AudioStreamPlaybackInteractive interactive)
            {
                interactive.SwitchToClipByName(clipName);
            }
        }
    }
    
    private AudioStreamPlayerRef GetPolyphonicPlayer()
    {
        if (!IsInstanceValid(_polyphonicPlayer))
        {
            _polyphonicPlayer = null;
        }
        
        switch (Use3D)
        {
            case false when _polyphonicPlayer is not AudioStreamPlayer:
            case true when _polyphonicPlayer is not AudioStreamPlayer3D:
                _polyphonicPlayer?.QueueFree();
                _polyphonicPlayer = null;
                break;
        }

        if (_polyphonicPlayer == null)
        {
            _polyphonicPlayer = Use3D ? new AudioStreamPlayer3D() : new AudioStreamPlayer();
            if(IsInsideTree() && !IsNodeReady())
                this.CallDeferred(Node.MethodName.AddChild, _polyphonicPlayer, (int)InternalMode.Front);
            else
                this.AddChild(_polyphonicPlayer, @internal: InternalMode.Front);
            // GD.Print("Created internal player");
        }
        
        CopyProperties(_polyphonicPlayer);

        return new AudioStreamPlayerRef(_polyphonicPlayer);
    }

    private AudioPlayerPoolInstance GetPool()
    {
        var pool = UseGlobalPool && GlobalAudioPlayerPool.InstanceExists
            ? GlobalAudioPlayerPool.Instance.Pool
            : this._internalPool;
        return pool;
    }

    private AudioStreamPlayerRef ClaimAudioPlayer()
    {
        Node usingNode = null;
        
        SanitizeAudioPlayerList();
        
        bool attemptReuse = _claimedPlayers.Count >= MaxPolyphony;
        if (attemptReuse)
        {
            {
                // 1st iteration, find players that aren't playing
                
                for (var index = 0; index < _claimedPlayers.Count; index++)
                {
                    var node = _claimedPlayers[index];
                    if (node is AudioStreamPlayer3D != Use3D) continue;
                    if (new AudioStreamPlayerRef(node).IsPlaying()) continue;
                    usingNode = node;
                    _claimedPlayers.RemoveAt(index);
                    break;
                }
            }

            if (usingNode == null)
            {
                // 2nd iteration, take any player
                for (var index = 0; index < _claimedPlayers.Count; index++)
                {
                    var node = _claimedPlayers[index];
                    if (node is AudioStreamPlayer3D != Use3D) continue;
                    usingNode = node;
                    _claimedPlayers.RemoveAt(index);
                    break;
                }
            }
            
        }

        if (usingNode == null)
        {
            if (Use3D)
                GetPool().Claim<AudioStreamPlayer3D>(ref usingNode);
            else
                GetPool().Claim<AudioStreamPlayer>(ref usingNode);
        }
        _claimedPlayers.Add(usingNode);

        return new AudioStreamPlayerRef(usingNode);
    }

    private void SanitizeAudioPlayerList()
    {
        for (int i = 0; i < _claimedPlayers.Count; i++)
        {
            var playerNode = _claimedPlayers[i];
            if (!IsInstanceValid(playerNode))
            {
                // Remove stale references
                _claimedPlayers.RemoveAt(i);
                i--;
                continue;
            }

            if ((playerNode is AudioStreamPlayer3D) != Use3D)
            {
                // Release and remove players that don't match Use3D
                GetPool().Release(ref playerNode);
                _claimedPlayers.RemoveAt(i);
                i--;
                continue;
            }

            if (i >= MaxPolyphony)
            {
                // Release and remove players that exceed MaxPolyphony
                GetPool().Release(ref playerNode);
                _claimedPlayers.RemoveAt(i);
                i--;
                continue;
            }
        }
    }


    private void CopyProperties(Node player)
    {
        if (player == null) return;
        player.Set(AudioStreamPlayer.PropertyName.VolumeDb, VolumeDb);
        player.Set(AudioStreamPlayer.PropertyName.PitchScale, PitchScale);
        player.Set(AudioStreamPlayer.PropertyName.Bus, Bus);
        player.Set(AudioStreamPlayer.PropertyName.MaxPolyphony, 1);
        player.Set("parameters/looping", ParameterLooping);
        player.ProcessMode = ProcessMode;

        if (Use3D)
        {
            CopyTransform(player, true);
            player.Set(AudioStreamPlayer3D.PropertyName.AttenuationModel, Variant.From(AttenuationModel));
            player.Set(AudioStreamPlayer3D.PropertyName.UnitSize, UnitSize);
            player.Set(AudioStreamPlayer3D.PropertyName.MaxDb, MaxDb);
            player.Set(AudioStreamPlayer3D.PropertyName.MaxDistance, MaxDistance);
            player.Set(AudioStreamPlayer3D.PropertyName.PanningStrength, PanningStrength);
            player.Set(AudioStreamPlayer3D.PropertyName.AreaMask, AreaMask);
            player.Set(AudioStreamPlayer3D.PropertyName.AttenuationFilterCutoffHz, AttenuationFilterCutoffHz);
            player.Set(AudioStreamPlayer3D.PropertyName.AttenuationFilterDb, AttenuationFilterDb);
        }
    }

    private void CopyTransform(Node player, bool isFirstTime)
    {
        if (Use3D && (isFirstTime || TrackPosition) && player is Node3D player3D && GetParentOrNull<Node3D>() is { } parent)
        {
            if (IsInsideTree())
                player3D.GlobalPosition = parent.GlobalPosition;
            else
                player3D.Position = Vector3.Zero;
        }
    }

    private void TestPrint(string msg)
    {
        if (IsInsideTree())
        {
            GD.Print($"{this.GetPath()}: {msg}");
        }
    }

    private void AddToActiveList()
    {
        if (_inActiveList) return;
        ActiveAudioPlayers.Add(this);
        _inActiveList = true;
    }

    private void RemoveFromActiveList()
    {
        if (_inActiveList)
        {
            _inActiveList = false;
            ActiveAudioPlayers.Remove(this);
        }
    }

    private bool FindDuplicatingAudioPlayer(out int matchingInstanceCount, out GdfAudioPlayer earliestMatchingInstance)
    {
        earliestMatchingInstance = null;
        matchingInstanceCount = 0;
        ulong now = Time.GetTicksMsec();
        foreach (var player in ActiveAudioPlayers)
        {
            if (player._lastPlayedStreamPath != this.Stream?.ResourcePath) continue;
            // same path

            earliestMatchingInstance ??= player;
            matchingInstanceCount++;

            if (now > player._lastPlayedTime + (ulong)(DeduplicationTimeFrameSec * 1_000)) continue;
            // within time frame
            
            return true;
        }

        return false;
    }

    private void ReleaseClaimedPlayers()
    {
        var pool = GetPool();
        // release all
        foreach (var playerNode in _claimedPlayers)
        {
            switch (ReleaseAction)
            {
                case ReleaseActionStop:
                    new AudioStreamPlayerRef(playerNode).Stop();
                    break;
                case ReleaseActionFadeOut:
                    var tween = new AudioStreamPlayerRef(playerNode).CreateTween();
                    tween.SetIgnoreTimeScale();
                    tween.TweenProperty(playerNode, new NodePath(AudioStreamPlayer.PropertyName.VolumeLinear), 0.0f, FadeOutTime);
                    tween.TweenCallback(new Callable(playerNode, AudioStreamPlayer.MethodName.Stop));
                    break;
            }

            var playerNodeToRelease = playerNode;
            pool.Release(ref playerNodeToRelease);
        }

        _claimedPlayers.Clear();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        bool isPlaying = IsPlaying();
        if (_inActiveList && !isPlaying)
        {
            RemoveFromActiveList();
            EmitSignalFinished();
        }
        if (Use3D && isPlaying)
        {
            // all
            foreach (var playerNode in _claimedPlayers)
            {
                CopyTransform(playerNode, false);
            }
        }
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        if(!_internalPool.IsValid) _internalPool = new(this);
        AnimationPlayer?.TryConnect(Godot.AnimationPlayer.SignalName.CurrentAnimationChanged, new Callable(this, MethodName.OnAnimationChanged));

        // if (Engine.IsEditorHint() && TriggerMode == TriggerModeEnum.Animation && AnimationPlayer == null)
        // {
        //     CallDeferred(MethodName.FindAndSetAnimationPlayer);
        // }
        //
        // if (Engine.IsEditorHint() && TriggerMode != TriggerModeEnum.Animation && FindAnimationPlayer(this, true) != null)
        // {
        //     GD.PrintErr($"An animation player is playing audio clips to a StandardAudioPlayer not set up with the Animation TriggerMode.\n{this.GetSceneAndPathString()}");
        // }
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        AnimationPlayer?.TryDisconnect(Godot.AnimationPlayer.SignalName.CurrentAnimationChanged, new Callable(this, MethodName.OnAnimationChanged));
        ReleaseClaimedPlayers();
        RemoveFromActiveList();
    }

    private void OnAnimationChanged(string name)
    {
        if (TriggerMode == TriggerModeEnum.Animation && StopOnAnimationChanged)
        {
            stop();
        }
    }

    public override void _ValidateProperty(Dictionary property)
    {
        base._ValidateProperty(property);
        if (!Engine.IsEditorHint()) return;
        
        var propName = property["name"].AsStringName();
        var usage = property["usage"].As<PropertyUsageFlags>();

        if (propName == PropertyName.TriggerMode || propName == PropertyName.Use3D || propName == PropertyName.ReleaseAction)
        {
            usage |= PropertyUsageFlags.UpdateAllIfModified;
        }

        if (propName == PropertyName.Stream)
        {
            if (TriggerMode == TriggerModeEnum.Animation)
            {
                usage |= PropertyUsageFlags.ReadOnly;
            }
        }

        if (propName == PropertyName.ParameterLooping)
        {
            usage |= PropertyUsageFlags.Checkable;
            property["type"] = (int)Variant.Type.Bool;
        }

        if (!Use3D && (
                propName == PropertyName.AttenuationModel ||
                propName == PropertyName.UnitSize ||
                propName == PropertyName.MaxDb ||
                propName == PropertyName.MaxDistance ||
                propName == PropertyName.PanningStrength ||
                propName == PropertyName.AreaMask ||
                propName == PropertyName.AttenuationFilterCutoffHz ||
                propName == PropertyName.AttenuationFilterDb))
        {
            usage &= ~PropertyUsageFlags.Storage;
        }

        if (TriggerMode != TriggerModeEnum.Animation && propName == PropertyName.AnimationPlayer)
        {
            usage &= ~(PropertyUsageFlags.Storage | PropertyUsageFlags.Editor);
        }

        if (TriggerMode != TriggerModeEnum.Animation && propName == PropertyName.ButtonFindAnimationPlayer)
        {
            usage &= ~(PropertyUsageFlags.Storage | PropertyUsageFlags.Editor);
        }

        if (TriggerMode != TriggerModeEnum.MethodCall && propName == PropertyName.ButtonPlay)
        {
            usage &= ~(PropertyUsageFlags.Storage | PropertyUsageFlags.Editor);
        }

        if (ReleaseAction != ReleaseActionFadeOut && propName == PropertyName.FadeOutTime)
        {
            usage &= ~(PropertyUsageFlags.Storage | PropertyUsageFlags.Editor);
        }

        if (propName == PropertyName.Bus)
        {
            property["hint_string"] = GetBusHintString();
        }

        if (propName == PropertyName.Playing)
        {
            usage &= ~PropertyUsageFlags.Storage;
        }

        property["usage"] = Variant.From(usage);
    }

    private static string GetBusHintString()
    {
        StringBuilder options = new();
        for (int i = 0; i < AudioServer.BusCount; i++) {
            if (i > 0) {
                options.Append(',');
            }
            string name = AudioServer.GetBusName(i);
            options.Append(name);
        }

        return options.ToString();
    }

    private void FindAndSetAnimationPlayer()
    {
#if TOOLS
        var foundAnimPlayer = FindAnimationPlayer(this);
        if (foundAnimPlayer == AnimationPlayer) return;
        var undoRedo = EditorInterface.Singleton.GetEditorUndoRedo();
        undoRedo.CreateAction($"Set {nameof(AnimationPlayer)}");
        undoRedo.AddDoProperty(this, PropertyName.AnimationPlayer, foundAnimPlayer);
        undoRedo.AddUndoProperty(this, PropertyName.AnimationPlayer, AnimationPlayer);
        undoRedo.CommitAction();
#endif
    }

    private static AnimationPlayer FindAnimationPlayer(GdfAudioPlayer audioPlayer, bool audioClipsOnly = false)
    {
        var sceneRoot = audioPlayer.Owner ?? audioPlayer;

        Queue<Node> searchQueue = new();
        searchQueue.Enqueue(sceneRoot);

        while (searchQueue.TryDequeue(out var node))
        {
            if (node != sceneRoot && node.Owner != sceneRoot) continue; // instance

            if (node is AnimationPlayer animationPlayer && DoesAnimationPlayerUseAudioPlayer(animationPlayer, audioPlayer, audioClipsOnly))
            {
                return animationPlayer;
            }

            foreach (var child in node.GetChildren())
            {
                searchQueue.Enqueue(child);
            }
        }

        return null;
    }

    private static bool DoesAnimationPlayerUseAudioPlayer(AnimationPlayer animationPlayer,
        GdfAudioPlayer audioPlayer, bool audioClipsOnly)
    {
        var animationPlayerRootNode = animationPlayer.GetNodeOrNull(animationPlayer.RootNode);
        if (animationPlayerRootNode == null) return false;
        var pathToAudioPlayer = animationPlayerRootNode.GetPathTo(audioPlayer);
        if (pathToAudioPlayer == null) return false;
        foreach (var animName in animationPlayer.GetAnimationList())
        {
            var anim = animationPlayer.GetAnimation(animName);
            for (int trackIdx = anim.GetTrackCount() - 1; trackIdx >= 0; trackIdx--)
            {
                if (anim.TrackGetPath(trackIdx) == pathToAudioPlayer)
                {
                    if (audioClipsOnly && anim.TrackGetType(trackIdx) != Animation.TrackType.Audio) continue;
                    // found something
                    return true;
                }
            }
        }

        return false;
    }

    private static readonly StringName MetaNameTransformedAudioPlayerTracks = "_transformed_audio_player_tracks";

    private static bool IsAlreadyTransformed(Resource res, NodePath pathToAudioPlayer)
    {
        Array<NodePath> memoryList;
        if (res.HasMeta(MetaNameTransformedAudioPlayerTracks))
        {
            memoryList = res.GetMeta(MetaNameTransformedAudioPlayerTracks).AsGodotArray<NodePath>();
            if (memoryList.Contains(pathToAudioPlayer)) return true;
        }
        else
        {
            memoryList = new();
            res.SetMeta(MetaNameTransformedAudioPlayerTracks, memoryList);
        }
        memoryList.Add(pathToAudioPlayer);

        return false;
    }
    
    private static void TransformAnimations(AnimationPlayer animationPlayer, GdfAudioPlayer audioPlayer)
    {
        var animationPlayerRootNode = animationPlayer?.GetNodeOrNull(animationPlayer.RootNode);
        if (animationPlayerRootNode == null) return;
        var pathToAudioPlayer = animationPlayerRootNode.GetPathTo(audioPlayer);
        if (pathToAudioPlayer == null) return;
        foreach (string animName in animationPlayer.GetAnimationList())
        {
            var anim = animationPlayer.GetAnimation(animName);
            TransformAnimation(anim, pathToAudioPlayer, audioPlayer);
        }
    }
    
    private static void TransformAnimation(Animation animation, NodePath pathToAudioPlayer, GdfAudioPlayer audioPlayer)
    {
        if (IsAlreadyTransformed(animation, pathToAudioPlayer)) return;

        for (int trackIdx = animation.GetTrackCount() - 1; trackIdx >= 0; trackIdx--)
        {
            if (animation.TrackGetType(trackIdx) == Animation.TrackType.Audio && animation.TrackGetPath(trackIdx) == pathToAudioPlayer)
            {
                int keyCount = animation.TrackGetKeyCount(trackIdx);
                if (keyCount <= 0) continue;
                if (!animation.TrackIsEnabled(trackIdx)) continue;
                int newMethodTrack = animation.AddTrack(Animation.TrackType.Method);
                animation.TrackSetPath(newMethodTrack, pathToAudioPlayer);

                for (int keyIdx = keyCount - 1; keyIdx >= 0; keyIdx--)
                {
                    double keyTime = animation.TrackGetKeyTime(trackIdx, keyIdx);

                    var stream = animation.AudioTrackGetKeyStream(trackIdx, keyIdx) as AudioStream;
                    float startOffset = animation.AudioTrackGetKeyStartOffset(trackIdx, keyIdx);
                    float endOffset = animation.AudioTrackGetKeyEndOffset(trackIdx, keyIdx);

                    var methodCallValue = new Dictionary();
                    methodCallValue["method"] = nameof(PlayClip);
                    methodCallValue["args"] = new Array()
                    {
                        stream, startOffset, endOffset
                    };

                    animation.TrackInsertKey(newMethodTrack, keyTime, methodCallValue);

                    if (audioPlayer.ReleaseAction is not ReleaseActionPlayToEnd)
                    {
                        // replace original audio track clips with empty stream (so we can still detect interruptions via set_stream null)
                        animation.AudioTrackSetKeyStream(trackIdx, keyIdx, audioPlayer.GetEmptyStream());
                    }
                    else
                    {
                        // disable original audio track
                        animation.TrackSetEnabled(trackIdx, false);
                    }
                }
            }
        }
    }

    private AudioStream _emptyStream;

    private AudioStream GetEmptyStream()
    {
        _emptyStream ??= new AudioStreamSynchronized();

        return _emptyStream;
    }

    public void PlayClip(AudioStream stream, float startOffset, float endOffset)
    {
        this.Stream = stream;
        play(startOffset);
        // Don't have any way to use endOffset, sorry
        // tho it's not like it worked before anyway
    }

    public void GetClipKeyframesInAnimation(AnimationPlayer animationPlayer, Animation animation, List<ClipKeyframeInfo> output)
    {
        var pathToAudioPlayer = animationPlayer?.GetNodeOrNull(animationPlayer.RootNode)?.GetPathTo(this);
        for (int trackIdx = animation.GetTrackCount() - 1; trackIdx >= 0; trackIdx--)
        {
            if (animation.TrackGetType(trackIdx) == Animation.TrackType.Method && animation.TrackGetPath(trackIdx) == pathToAudioPlayer)
            {
                int keyCount = animation.TrackGetKeyCount(trackIdx);
                if (keyCount <= 0) continue;
                if (!animation.TrackIsEnabled(trackIdx)) continue;
                
                for (int keyIdx = keyCount - 1; keyIdx >= 0; keyIdx--)
                {
                    var methodCallValue = animation.TrackGetKeyValue(trackIdx, keyIdx).AsGodotDictionary();
                    if (methodCallValue["method"].AsString() != nameof(PlayClip)) continue;
                    var args = methodCallValue["args"].AsGodotArray();
                    var keyInfo = new ClipKeyframeInfo
                    {
                        Time = animation.TrackGetKeyTime(trackIdx, keyIdx),
                        Stream = args[0].As<AudioStream>(),
                        StartOffset = args[1].AsSingle(),
                        EndOffset = args[2].AsSingle()
                    };
                    
                    output.Add(keyInfo);
                }
            }
        }
    }

    public override void _Notification(int what)
    {
        if (what == GdfConstants.NotificationDeepSceneInstantiated)
        {
            this.ConnectToSceneInstantiatedSignal(new Callable(this, MethodName.PostInitialize));
        }
    }

    private void TestAnimationTransformation()
    {
        TransformAnimations(AnimationPlayer, this);
    }

    private void PostInitialize()
    {
        _internalPool = new(this);
        // Instantiate and add internal audio player, so that it's ready in case of autoplay
        if (!UseGlobalPool)
        {
            ClaimAudioPlayer();
            ReleaseClaimedPlayers();
        }
        
        // Instantiate and add polyphonic audio player, so that it's ready in case of animation autoplay
        if (TriggerMode == TriggerModeEnum.Animation)
            GetPolyphonicPlayer();
        
        if (TriggerMode == TriggerModeEnum.Animation && AnimationPlayer != null && !Engine.IsEditorHint())
        {
            TransformAnimations(AnimationPlayer, this);
        }
    }

    public struct ClipKeyframeInfo
    {
        public double Time;
        public AudioStream Stream;
        public float StartOffset;
        public float EndOffset;
    }
}

public enum TriggerModeEnum
{
    MethodCall,
    Animation,
    Autoplay
}