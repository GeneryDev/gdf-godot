using System;
using GDF.Logical.Values;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

namespace GDF.Logical.Signals;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/signal_port_in.png")]
public partial class SignalPortInbound : SignalPort, IInboundArgumentSource
{
    [Signal]
    public delegate void ReceivedEventHandler();

    private Array _receivingArgs;

    [Export] public Array<ObjectCallable> Callables = new();

    public void Receive(Array args)
    {
        _receivingArgs = args;
        try
        {
            EmitSignalReceived();
            InvokeCallables();
        }
        finally
        {
            _receivingArgs = null;
        }
    }

    public Variant GetArgument(int index)
    {
        if (_receivingArgs == null || index < 0 || index >= _receivingArgs.Count) return default;
        return _receivingArgs[index];
    }

    private void InvokeCallables()
    {
        foreach (var callable in Callables)
        {
            if (callable == null) continue;
            try
            {
                callable.Call(this);
            }
            catch (Exception ex)
            {
                GD.PushError(ex.Message);
            }
        }
    }
}