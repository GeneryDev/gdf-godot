using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace GDF.Audio;

public struct AudioPlayerPoolInstance
{
    public static readonly StringName MetaNameAudioTweens = "_audio_tweens";

    public Node ParentNode;
    
    private readonly List<Node> _claimedPlayers = new();
    private readonly List<Node> _availablePlayers = new();
    private readonly List<Node> _playersAwaitingFinish = new();
    
    public bool IsValid => ParentNode != null;

    public AudioPlayerPoolInstance(Node parentNode)
    {
        ParentNode = parentNode;
    }

    public void Claim<T>(ref Node node) where T : Node, new()
    {
        if (node != null)
        {
            GD.PrintErr("Cannot claim an audio player; the target member already has a non-null player.");
            return;
        }
        if (_availablePlayers is { Count: > 0 })
        {
            int availableIndex = FindOfType<T>(_availablePlayers);

            if (availableIndex != -1)
            {
                var reclaimed = (T)_availablePlayers[availableIndex];
                _availablePlayers.RemoveAt(availableIndex);
                _claimedPlayers.Add(reclaimed);
                Reset(reclaimed);
                node = reclaimed;
                return;
            }
        }
        
        // none available, create.
        var created = new T();
        ParentNode.AddChild(created);
        _claimedPlayers.Add(created);
        Reset(created); 
        node = created;
    }

    public void Release(ref Node node)
    {
        if (node == null) return;
        int claimedIndex = _claimedPlayers?.IndexOf(node) ?? -1;
        node = null;
        if (_claimedPlayers != null && claimedIndex != -1)
        {
            var released = _claimedPlayers[claimedIndex];
            _claimedPlayers.RemoveAt(claimedIndex);
            if (new AudioStreamPlayerRef(released).IsPlaying())
            {
                _playersAwaitingFinish.Add(released);
            }
            else
            {
                _availablePlayers.Add(released);
                Reset(released);
            }
        }
        
        // not claimed.
    }

    public void Tick(double delta)
    {
        if (_playersAwaitingFinish.Count > 0)
        {
            for (int i = 0; i < _playersAwaitingFinish.Count; i++)
            {
                var player = _playersAwaitingFinish[i];
                if (!new AudioStreamPlayerRef(player).IsPlaying())
                {
                    _playersAwaitingFinish.RemoveAt(i);
                    _availablePlayers.Add(player);
                    Reset(player);
                    i--;
                    continue;
                }
            }
        }
    }

    private void Reset<T>(T node) where T : Node
    {
        switch (node)
        {
            case AudioStreamPlayer player:
                player.Stream = null;
                break;
            case AudioStreamPlayer3D player:
                player.Stream = null;
                break;
        }
        
        if (node.HasMeta(MetaNameAudioTweens))
        {
            var tweens = node.GetMeta(MetaNameAudioTweens).AsGodotArray<Tween>();
            foreach (var tween in tweens)
            {
                tween.Kill();
            }
            tweens.Clear();
        }
    }

    private int FindOfType<T>(List<Node> nodes) where T : Node
    {
        for (int i = nodes.Count - 1; i >= 0; i--)
        {
            if (nodes[i] is T t) return i;
        }

        return -1;
    }
}

public struct AudioStreamPlayerRef
{
    public Node Player;

    public AudioStreamPlayerRef(Node player)
    {
        Player = player;
    }

    public AudioStream GetStream()
    {
        if (Player == null) return null;
        switch (Player)
        {
            case AudioStreamPlayer player:
                return player.GetStream();
            case AudioStreamPlayer2D player:
                return player.GetStream();
            case AudioStreamPlayer3D player:
                return player.GetStream();
        }

        return null;
    }

    public void SetStream(AudioStream stream)
    {
        if (Player == null) return;
        switch (Player)
        {
            case AudioStreamPlayer player:
                player.SetStream(stream);
                break;
            case AudioStreamPlayer2D player:
                player.SetStream(stream);
                break;
            case AudioStreamPlayer3D player:
                player.SetStream(stream);
                break;
        }
    }

    public bool IsPlaying()
    {
        if (Player == null) return false;
        switch (Player)
        {
            case AudioStreamPlayer player:
                return player.IsPlaying();
            case AudioStreamPlayer2D player:
                return player.IsPlaying();
            case AudioStreamPlayer3D player:
                return player.IsPlaying();
        }

        return false;
    }

    public void Play(float fromPosition = 0)
    {
        if (Player == null) return;
        switch (Player)
        {
            case AudioStreamPlayer player:
                player.Play(fromPosition);
                break;
            case AudioStreamPlayer2D player:
                player.Play(fromPosition);
                break;
            case AudioStreamPlayer3D player:
                player.Play(fromPosition);
                break;
        }
    }

    public void Stop()
    {
        if (Player == null) return;
        switch (Player)
        {
            case AudioStreamPlayer player:
                player.Stop();
                break;
            case AudioStreamPlayer2D player:
                player.Stop();
                break;
            case AudioStreamPlayer3D player:
                player.Stop();
                break;
        }
    }

    public bool HasStreamPlayback()
    {
        if (Player == null) return false;
        switch (Player)
        {
            case AudioStreamPlayer player:
                return player.HasStreamPlayback();
            case AudioStreamPlayer2D player:
                return player.HasStreamPlayback();
            case AudioStreamPlayer3D player:
                return player.HasStreamPlayback();
        }

        return false;
    }

    public AudioStreamPlayback GetStreamPlayback()
    {
        if (Player == null) return null;
        switch (Player)
        {
            case AudioStreamPlayer player:
                return player.GetStreamPlayback();
            case AudioStreamPlayer2D player:
                return player.GetStreamPlayback();
            case AudioStreamPlayer3D player:
                return player.GetStreamPlayback();
        }

        return null;
    }

    public StringName GetBus()
    {
        if (Player == null) return null;
        switch (Player)
        {
            case AudioStreamPlayer player:
                return player.GetBus();
            case AudioStreamPlayer2D player:
                return player.GetBus();
            case AudioStreamPlayer3D player:
                return player.GetBus();
        }

        return null;
    }

    public AudioServer.PlaybackType GetPlaybackType()
    {
        if (Player == null) return default;
        switch (Player)
        {
            case AudioStreamPlayer player:
                return player.GetPlaybackType();
            case AudioStreamPlayer2D player:
                return player.GetPlaybackType();
            case AudioStreamPlayer3D player:
                return player.GetPlaybackType();
        }

        return default;
    }

    public Tween CreateTween()
    {
        if (Player == null) return null;
        var tween = Player.CreateTween();
        Array<Tween> tweens; 
        if (Player.HasMeta(AudioPlayerPoolInstance.MetaNameAudioTweens))
        {
            tweens = Player.GetMeta(AudioPlayerPoolInstance.MetaNameAudioTweens).AsGodotArray<Tween>();
        }
        else
        {
            tweens = new Array<Tween>();
            Player.SetMeta(AudioPlayerPoolInstance.MetaNameAudioTweens, tweens);
        }
        tweens.Add(tween);
        return tween;
    }
}