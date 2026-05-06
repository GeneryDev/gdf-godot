using GDF.Data;
using GDF.Logical.Signals;
using GDF.Util;
using Godot;

namespace GDF.UI;

public partial class ScreenPlaceholder : IDataContextInjectable, ISignalStationConnectable
{
    public bool CanInjectContext(StringName injectingSlotId)
    {
        return true;
    }

    public void InjectContext(StringName slotId, IDataContext itemContext)
    {
        _screen.InjectContext(slotId, itemContext);
    }

    public void ConnectSignalStation(SignalStation station)
    {
        _screen.ConnectSignalStation(station);
    }
}