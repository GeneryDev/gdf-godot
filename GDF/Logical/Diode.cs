using Godot;

namespace GDF.Logical;

[Tool]
[GlobalClass]
[Icon(GdfIcons.LogicThrough)]
public partial class Diode : TriggerableLogicNode
{
    public void Trigger()
    {
        HandleTrigger();
    }
}