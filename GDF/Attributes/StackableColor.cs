using System;
using Godot;

namespace GDF.Attributes;

public class StackableColor: StandardStackableValue<Color>
{
    public const int AddModeAdd = 0;
    public const int AddModeBlend = 1;

    public int AddMode = AddModeAdd;

    public StackableColor() {}
    public StackableColor(Color baseValue) : base(baseValue) {}

    public override Color ApplyOperation(Color a, Color b, ModificationOperator op)
    {
        switch (op)
        {
            case ModificationOperator.Set:
                return b;
            case ModificationOperator.Add:
                return AddMode switch
                {
                    AddModeAdd => a + b,
                    AddModeBlend => b.Blend(a),
                    _ => a + b
                };
            case ModificationOperator.Multiply:
                return a * b;
            case ModificationOperator.And:
            case ModificationOperator.Or:
            default:
                throw new NotSupportedException();
        }
    }

    protected override Color Lerp(Color a, Color b, float weight)
    {
        return a.Lerp(b, weight);
    }
}