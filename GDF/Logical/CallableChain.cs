using System;
using GDF.Logical.Signals;
using GDF.Logical.Values;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

namespace GDF.Logical;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/logic_multi_out_ordered.png")]
public partial class CallableChain : TriggerableLogicNode, IInboundArgumentSource
{
    private Array _receivingArgs;

    [Export] public Array<ObjectCallable> Callables = new();

    public void Trigger()
    {
        HandleTrigger();
    }

    protected override Empty Execute()
    {
        Callv(null);
        return base.Execute();
    }

    public void Call()
    {
        Callv(null);
    }

    public void Call(Variant p0)
    {
        Callv(new Array {p0});
    }

    public void Call(Variant p0, Variant p1)
    {
        Callv(new Array {p0, p1});
    }

    public void Call(Variant p0, Variant p1, Variant p2)
    {
        Callv(new Array {p0, p1, p2});
    }

    public void Call(Variant p0, Variant p1, Variant p2, Variant p3)
    {
        Callv(new Array {p0, p1, p2, p3});
    }

    public void Call(Variant p0, Variant p1, Variant p2, Variant p3, Variant p4)
    {
        Callv(new Array {p0, p1, p2, p3, p4});
    }

    public void Call(Variant p0, Variant p1, Variant p2, Variant p3, Variant p4, Variant p5)
    {
        Callv(new Array {p0, p1, p2, p3, p4, p5});
    }

    public void Call(Variant p0, Variant p1, Variant p2, Variant p3, Variant p4, Variant p5, Variant p6)
    {
        Callv(new Array {p0, p1, p2, p3, p4, p5, p6});
    }

    public void Call(Variant p0, Variant p1, Variant p2, Variant p3, Variant p4, Variant p5, Variant p6, Variant p7)
    {
        Callv(new Array {p0, p1, p2, p3, p4, p5, p6, p7});
    }

    public void Call(Variant p0, Variant p1, Variant p2, Variant p3, Variant p4, Variant p5, Variant p6, Variant p7,
        Variant p8)
    {
        Callv(new Array {p0, p1, p2, p3, p4, p5, p6, p7, p8});
    }

    public void Callv(Array args)
    {
        _receivingArgs = args;
        try
        {
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