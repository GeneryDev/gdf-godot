using Godot;

namespace GDF.Util;

public struct UnscaledTimer
{
    private ulong _lastTimeUs;
    private ulong _startTimeUs;
    public double Delta;

    public float ElapsedTime => (_lastTimeUs - _startTimeUs) / 1000000f;

    public void Tick()
    {
        ulong currentTimeUs = Time.GetTicksUsec();
        Delta = (currentTimeUs - _lastTimeUs) / 1000000d;
        _lastTimeUs = currentTimeUs;
    }

    public void Restart()
    {
        _startTimeUs = _lastTimeUs = Time.GetTicksUsec();
        Delta = 0;
    }
}