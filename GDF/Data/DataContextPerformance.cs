using GDF.Util;
using Godot;

namespace GDF.Data;

public partial class DataContextPerformance : SingletonNode<DataContextPerformance>
{
    public int AccumulatedBindingUpdates = 0;
    private int _bindingUpdatesLastSecond = 0;
    
    public override void _Ready()
    {
        base._Ready();
        Performance.Singleton.AddCustomMonitor("data_contexts/binding_updates_per_second", new Callable(this, MethodName.GetBindingUpdatesPerSecond));
        var timer = new Timer()
        {
            WaitTime = 1,
            Autostart = true,
            IgnoreTimeScale = true,
            OneShot = false,
            ProcessMode = ProcessModeEnum.Always
        };
        timer.Timeout += UpdateMonitors;
        this.AddChild(timer);
    }

    private void UpdateMonitors()
    {
        _bindingUpdatesLastSecond = AccumulatedBindingUpdates;
        AccumulatedBindingUpdates = 0;
    }

    public int GetBindingUpdatesPerSecond()
    {
        return _bindingUpdatesLastSecond;
    }
}