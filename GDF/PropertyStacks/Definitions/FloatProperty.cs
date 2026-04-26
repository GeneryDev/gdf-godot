using Godot;

namespace GDF.PropertyStacks.Definitions;

[GlobalClass]
[Tool]
public partial class FloatProperty : StandardPropertyDefinition<float>
{
    public const int InterpolationModeLinear = 0;
    public const int InterpolationModeRadians = 1;
    public const int InterpolationModeDegrees = 2;
    
    [Export] public float DefaultValue;
    [Export(PropertyHint.Enum,"Linear,Radians,Degrees")] public int InterpolationMode;

    public override float GetDefaultValue()
    {
        return DefaultValue;
    }
    
    public override VectorModification<float> InputToIntermediate(object input)
    {
        if (input is int i) return new VectorModification<float>() { Value = i, Operation = DefaultOperator};
        if (input is double d) return new VectorModification<float>() { Value = (float)d, Operation = DefaultOperator };
        return base.InputToIntermediate(input);
    }

    public override float ApplyAdd(float a, float b)
    {
        return a + b;
    }
    public override float ApplyMultiply(float a, float b)
    {
        return a * b;
    }

    public override float Lerp(float a, float b, float weight)
    {
        switch (InterpolationMode)
        {
            case InterpolationModeRadians:
                return Mathf.LerpAngle(a, b, weight);
            case InterpolationModeDegrees:
                return Mathf.RadToDeg(Mathf.LerpAngle(Mathf.DegToRad(a), Mathf.DegToRad(b), weight));
        }
        return Mathf.Lerp(a, b, weight);
    }

    public override Variant OutputToVariant(float value)
    {
        return Variant.From(value);
    }
}