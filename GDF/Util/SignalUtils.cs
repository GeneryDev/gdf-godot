using GDF.Logical.Signals;
using Godot;

namespace GDF.Util;

public static class SignalUtils
{
    public static void ConnectSignalStation(this Node node, SignalStation station)
    {
        if (station == null) return;
        if (node is ISignalStationConnectable connectable)
        {
            connectable.ConnectSignalStation(station);
            return;
        }
        node.GetChildOfType<SignalStation>()?.ConnectStationTwoWay(station);

        foreach (var port in station.IterateChildrenOfType<SignalPortInbound>())
        {
            var signalName = port.Name;

            if (node.HasUserSignal(signalName) || node.HasSignal(signalName))
            {
                var adapter = SignalAdapter.Receiver(args =>
                {
                    port.Receive(args);
                });
                adapter.ConnectReceiveAndBind(node, signalName, port);
            }
        }
    }
}