using GDF.Logical.Values;
using Godot;

namespace GDF.Logical.Signals;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/method_argument.png")]
public partial class InboundSignalArgument : ValueSource
{
    [Export] public int ArgumentIndex;
    
    public override Variant GetValue(Node source)
    {
        if (source is IInboundArgumentSource inbound) return inbound.GetArgument(ArgumentIndex);
        
        GD.PushError($"Cannot use {nameof(InboundSignalArgument)} value source from any node other than a {nameof(SignalPortInbound)} node or node extending {nameof(IInboundArgumentSource)}.");
        return default;
    }

    public override string ToString()
    {
        return $"<arg {ArgumentIndex}>";
    }
}