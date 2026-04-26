using Godot;

namespace GDF.Logical;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/logic_through.png")]
public partial class Diode : TriggerableLogicNode
{
    public void Trigger()
    {
        HandleTrigger();
    }
}