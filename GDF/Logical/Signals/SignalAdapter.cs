using System;
using Godot;
using Array = Godot.Collections.Array;

namespace GDF.Logical.Signals;

public partial class SignalAdapter : GodotObject
{
    public GodotObject NewEmitter;
    public StringName EmittedSignalName;
    public Callable EmitCallable => new(this, MethodName.Emit);
    public Callable ReceiveCallable => new(this, MethodName.Receive);
    private Action<Array> _receiveCallback;
    
    private GodotObject _connectedObject;
    private StringName _connectedSignalName;
    private GodotObject _boundObject;

    private SignalAdapter()
    {
    }

    public static SignalAdapter Emitter(GodotObject newEmitter, StringName emittedSignalName)
    {
        return new SignalAdapter()
        {
            NewEmitter = newEmitter,
            EmittedSignalName = emittedSignalName
        };
    }

    public static SignalAdapter Receiver(Action<Array> callback)
    {
        return new SignalAdapter()
        {
            _receiveCallback = callback
        };
    }

    public void Emit()
    {
        if (NewEmitter != null &&
            (NewEmitter.HasUserSignal(EmittedSignalName) || NewEmitter.HasSignal(EmittedSignalName)))
            NewEmitter.EmitSignal(EmittedSignalName);
    }

    public void Emit(Variant p0)
    {
        if (NewEmitter != null &&
            (NewEmitter.HasUserSignal(EmittedSignalName) || NewEmitter.HasSignal(EmittedSignalName)))
            NewEmitter.EmitSignal(EmittedSignalName, p0);
    }

    public void Emit(Variant p0, Variant p1)
    {
        if (NewEmitter != null &&
            (NewEmitter.HasUserSignal(EmittedSignalName) || NewEmitter.HasSignal(EmittedSignalName)))
            NewEmitter.EmitSignal(EmittedSignalName, p0, p1);
    }

    public void Emit(Variant p0, Variant p1, Variant p2)
    {
        if (NewEmitter != null &&
            (NewEmitter.HasUserSignal(EmittedSignalName) || NewEmitter.HasSignal(EmittedSignalName)))
            NewEmitter.EmitSignal(EmittedSignalName, p0, p1, p2);
    }

    public void Emit(Variant p0, Variant p1, Variant p2, Variant p3)
    {
        if (NewEmitter != null &&
            (NewEmitter.HasUserSignal(EmittedSignalName) || NewEmitter.HasSignal(EmittedSignalName)))
            NewEmitter.EmitSignal(EmittedSignalName, p0, p1, p2, p3);
    }

    public void Emit(Variant p0, Variant p1, Variant p2, Variant p3, Variant p4)
    {
        if (NewEmitter != null &&
            (NewEmitter.HasUserSignal(EmittedSignalName) || NewEmitter.HasSignal(EmittedSignalName)))
            NewEmitter.EmitSignal(EmittedSignalName, p0, p1, p2, p3, p4);
    }

    public void Emit(Variant p0, Variant p1, Variant p2, Variant p3, Variant p4, Variant p5)
    {
        if (NewEmitter != null &&
            (NewEmitter.HasUserSignal(EmittedSignalName) || NewEmitter.HasSignal(EmittedSignalName)))
            NewEmitter.EmitSignal(EmittedSignalName, p0, p1, p2, p3, p4, p5);
    }

    public void Emit(Variant p0, Variant p1, Variant p2, Variant p3, Variant p4, Variant p5, Variant p6)
    {
        if (NewEmitter != null &&
            (NewEmitter.HasUserSignal(EmittedSignalName) || NewEmitter.HasSignal(EmittedSignalName)))
            NewEmitter.EmitSignal(EmittedSignalName, p0, p1, p2, p3, p4, p5, p6);
    }

    public void Emit(Variant p0, Variant p1, Variant p2, Variant p3, Variant p4, Variant p5, Variant p6, Variant p7)
    {
        if (NewEmitter != null &&
            (NewEmitter.HasUserSignal(EmittedSignalName) || NewEmitter.HasSignal(EmittedSignalName)))
            NewEmitter.EmitSignal(EmittedSignalName, p0, p1, p2, p3, p4, p5, p6, p7);
    }

    public void Emit(Variant p0, Variant p1, Variant p2, Variant p3, Variant p4, Variant p5, Variant p6, Variant p7,
        Variant p8)
    {
        if (NewEmitter != null &&
            (NewEmitter.HasUserSignal(EmittedSignalName) || NewEmitter.HasSignal(EmittedSignalName)))
            NewEmitter.EmitSignal(EmittedSignalName, p0, p1, p2, p3, p4, p5, p6, p7, p8);
    }


    public void Receive()
    {
        if (PrepareReceive())
            _receiveCallback(new Array());
    }

    public void Receive(Variant p0)
    {
        if (PrepareReceive())
            _receiveCallback(new Array() { p0 });
    }

    public void Receive(Variant p0, Variant p1)
    {
        if (PrepareReceive())
            _receiveCallback(new Array() { p0, p1 });
    }

    public void Receive(Variant p0, Variant p1, Variant p2)
    {
        if (PrepareReceive())
            _receiveCallback(new Array() { p0, p1, p2 });
    }

    public void Receive(Variant p0, Variant p1, Variant p2, Variant p3)
    {
        if (PrepareReceive())
            _receiveCallback(new Array() { p0, p1, p2, p3 });
    }

    public void Receive(Variant p0, Variant p1, Variant p2, Variant p3, Variant p4)
    {
        if (PrepareReceive())
            _receiveCallback(new Array() { p0, p1, p2, p3, p4 });
    }

    public void Receive(Variant p0, Variant p1, Variant p2, Variant p3, Variant p4, Variant p5)
    {
        if (PrepareReceive())
            _receiveCallback(new Array() { p0, p1, p2, p3, p4, p5 });
    }

    public void Receive(Variant p0, Variant p1, Variant p2, Variant p3, Variant p4, Variant p5, Variant p6)
    {
        if (PrepareReceive())
            _receiveCallback(new Array() { p0, p1, p2, p3, p4, p5, p6 });
    }

    public void Receive(Variant p0, Variant p1, Variant p2, Variant p3, Variant p4, Variant p5, Variant p6, Variant p7)
    {
        if (PrepareReceive())
            _receiveCallback(new Array() { p0, p1, p2, p3, p4, p5, p6, p7 });
    }

    public void Receive(Variant p0, Variant p1, Variant p2, Variant p3, Variant p4, Variant p5, Variant p6, Variant p7,
        Variant p8)
    {
        if (PrepareReceive())
            _receiveCallback(new Array() { p0, p1, p2, p3, p4, p5, p6, p7, p8 });
    }

    private bool PrepareReceive()
    {
        if (_boundObject != null)
        {
            if (!IsInstanceValid(_boundObject))
            {
                // Receiver bound object is now stale.
                if (IsInstanceValid(_connectedObject))
                {
                    // Attempt to disconnect the signal this adapter was connected to.
                    _connectedObject.Disconnect(_connectedSignalName, this.ReceiveCallable);
                }
                return false;
            }
        }
        return true;
    }

    public void BindToObject(GodotObject obj)
    {
        _boundObject = obj;
    }

    public void ConnectReceiveAndBind(GodotObject source, StringName signalName, GodotObject boundObject)
    {
        source.Connect(signalName, this.ReceiveCallable);
        _boundObject = boundObject;
        _connectedObject = source;
        _connectedSignalName = signalName;
    }
}