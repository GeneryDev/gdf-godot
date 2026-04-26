using Godot;
using Godot.Collections;

namespace GDF.Logical.Signals;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/signal_port_out.png")]
public partial class SignalPortOutbound : SignalPort
{
    public void Emitv(Array args)
    {
        GetParent<SignalStation>()?.Emit(this.Name, args);
    }

    public void Emit()
    {
        Emitv(null);
    }

    public void Emit(Variant p0)
    {
        Emitv(new Array {p0});
    }

    public void Emit(Variant p0, Variant p1)
    {
        Emitv(new Array {p0, p1});
    }

    public void Emit(Variant p0, Variant p1, Variant p2)
    {
        Emitv(new Array {p0, p1, p2});
    }

    public void Emit(Variant p0, Variant p1, Variant p2, Variant p3)
    {
        Emitv(new Array {p0, p1, p2, p3});
    }

    public void Emit(Variant p0, Variant p1, Variant p2, Variant p3, Variant p4)
    {
        Emitv(new Array {p0, p1, p2, p3, p4});
    }

    public void Emit(Variant p0, Variant p1, Variant p2, Variant p3, Variant p4, Variant p5)
    {
        Emitv(new Array {p0, p1, p2, p3, p4, p5});
    }

    public void Emit(Variant p0, Variant p1, Variant p2, Variant p3, Variant p4, Variant p5, Variant p6)
    {
        Emitv(new Array {p0, p1, p2, p3, p4, p5, p6});
    }

    public void Emit(Variant p0, Variant p1, Variant p2, Variant p3, Variant p4, Variant p5, Variant p6, Variant p7)
    {
        Emitv(new Array {p0, p1, p2, p3, p4, p5, p6, p7});
    }

    public void Emit(Variant p0, Variant p1, Variant p2, Variant p3, Variant p4, Variant p5, Variant p6, Variant p7,
        Variant p8)
    {
        Emitv(new Array {p0, p1, p2, p3, p4, p5, p6, p7, p8});
    }
}