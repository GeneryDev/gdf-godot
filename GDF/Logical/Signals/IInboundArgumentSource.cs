using Godot;

namespace GDF.Logical.Signals;

public interface IInboundArgumentSource
{
    public Variant GetArgument(int index);
}