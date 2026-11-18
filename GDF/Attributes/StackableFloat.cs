using System;
using Godot;

namespace GDF.Attributes;

public class StackableFloat: StandardStackableValue<float>
{
    public const int InterpolationModeLinear = 0;
    public const int InterpolationModeRadians = 1;
    public const int InterpolationModeDegrees = 2;

    public int InterpolationMode = InterpolationModeLinear;

    public StackableFloat() {}
    public StackableFloat(float baseValue) : base(baseValue) {}

    public override float ApplyOperation(float a, float b, ModificationOperator op)
    {
        switch (op)
        {
            case ModificationOperator.Set:
                return b;
            case ModificationOperator.Add:
                return a + b;
            case ModificationOperator.Multiply:
                return a * b;
            case ModificationOperator.And:
            case ModificationOperator.Or:
            default:
                throw new NotSupportedException();
        }
    }

    protected override float Lerp(float a, float b, float weight)
    {
        return InterpolationMode switch
        {
            InterpolationModeRadians => Mathf.LerpAngle(a, b, weight),
            InterpolationModeDegrees => Mathf.RadToDeg(Mathf.LerpAngle(Mathf.DegToRad(a), Mathf.DegToRad(b), weight)),
            _ => Mathf.Lerp(a, b, weight)
        };
    }
}