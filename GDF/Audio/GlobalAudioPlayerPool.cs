using GDF.Util;
using Godot;

namespace GDF.Audio;

[GlobalClass]
[SingletonUsage(SingletonUsage.Autoload)]
public partial class GlobalAudioPlayerPool : SingletonNode<GlobalAudioPlayerPool>
{
    private AudioPlayerPoolInstance _pool;

    public AudioPlayerPoolInstance Pool => _pool;

    public override void _Ready()
    {
        _pool = new AudioPlayerPoolInstance(this);
        base._Ready();
    }

    public void Claim<T>(ref Node node) where T : Node, new()
    {
        _pool.Claim<T>(ref node);
    }

    public void Release(ref Node node)
    {
        _pool.Release(ref node);
    }

    public override void _Process(double delta)
    {
        _pool.Tick(delta);
    }
}