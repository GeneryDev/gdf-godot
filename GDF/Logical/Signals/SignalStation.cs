using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

namespace GDF.Logical.Signals;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/signal_station.png")]
public partial class SignalStation : Node
{
    private Array<SignalStation> _connectedStations = new();

    private System.Collections.Generic.Dictionary<StringName, List<Callable>> _directOutboundConnections;
    private System.Collections.Generic.Dictionary<StringName, List<Callable>> _directInboundConnections;

    public void Emit(StringName portName)
    {
        Emit(portName, null);
    }
    
    public void Emit(StringName portName, Array args)
    {
        InvokeDirectCallbacks(_directOutboundConnections, portName, args);
        foreach (var station in _connectedStations)
        {
            station.Receive(portName, args);
        }
    }

    public void Receive(StringName portName)
    {
        Receive(portName, null);
    }

    public void Receive(StringName portName, Array args)
    {
        var ownPort = this.GetNodeOrNull<SignalPort>(new NodePath(portName));
        if (ownPort != null && ownPort.GetParent() == this)
        {
            if (ownPort is SignalPortInbound inbound && ownPort.Name == portName)
            {
                inbound.Receive(args);
            }
        }
        InvokeDirectCallbacks(_directInboundConnections, portName, args);
    }

    public void ConnectStation(SignalStation station)
    {
        if (station == null) return;
        if (!IsInstanceValid(station)) return;
        if (_connectedStations.Contains(station)) return;
        _connectedStations.Add(station);
    }

    public void ConnectStationTwoWay(SignalStation station)
    {
        if (station == null) return;
        ConnectStation(station);
        station.ConnectStation(this);
    }

    public void DisconnectStation(SignalStation station)
    {
        if (station == null) return;
        _connectedStations.Remove(station);
    }

    public void DisconnectStationTwoWay(SignalStation station)
    {
        if (station == null) return;
        DisconnectStation(station);
        station.DisconnectStation(this);
    }

    public void DisconnectAll()
    {
        _connectedStations.Clear();
    }

    public void DisconnectAllTwoWay()
    {
        foreach (var station in _connectedStations)
        {
            if(station != this)
                station.DisconnectStation(this);
        }
        _connectedStations.Clear();
    }
    

    public override void _Notification(int what)
    {
        if (what == NotificationPredelete)
        {
            DisconnectAllTwoWay();
        }
    }

    public void ConnectDirectOutbound(StringName portName, Callable callable)
    {
        ConnectDirect(ref _directOutboundConnections, portName, callable);
    }

    public void ConnectDirectInbound(StringName portName, Callable callable)
    {
        ConnectDirect(ref _directInboundConnections, portName, callable);
    }

    private static void ConnectDirect(ref System.Collections.Generic.Dictionary<StringName, List<Callable>> dictRef,
        StringName portName, Callable callable)
    {
        dictRef ??= new();
        if (!dictRef.TryGetValue(portName, out var list))
        {
            list = dictRef[portName] = new();
        }
        if(!list.Contains(callable)) list.Add(callable);
    }
    private static void InvokeDirectCallbacks(System.Collections.Generic.Dictionary<StringName, List<Callable>> dict,
        StringName portName, Array args)
    {
        if (dict?.TryGetValue(portName, out var directCallables) ?? false)
        {
            var argsArr = args?.ToArray() ?? System.Array.Empty<Variant>();
            foreach (var callable in directCallables)
            {
                callable.Call(argsArr);
            }
        }
    }
}