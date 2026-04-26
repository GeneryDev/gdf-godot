using Godot;

namespace GDF.Logical.Values;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/any_of.png")]
public partial class AnyOfValues : ValueSource
{
    [Export]
    public ValueSource[] Operands;
    
    public override Variant GetValue(Node source)
    {
        if (Operands != null)
        {
            foreach (var operand in Operands)
            {
                if (operand.GetValue<bool>(source)) return true;
            }
        }
        return false;
    }
}