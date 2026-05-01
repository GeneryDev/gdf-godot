using GDF.PropertyStacks.Internal;
using Godot;

namespace GDF.PropertyStacks.Definitions;

[GlobalClass]
[Tool]
public partial class FloatProperty : StandardPropertyDefinition<float>,
    IPropertyAcceptsInput<float, VectorModification<float>>,
    IPropertyAcceptsInput<int, VectorModification<float>>,
    IPropertyAcceptsInput<VectorModification<float>, VectorModification<float>>
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

    public VectorModification<float> InputToIntermediate(float input)
    {
        return new VectorModification<float>() { Value = input, Operation = DefaultOperator };
    }

    public VectorModification<float> InputToIntermediate(int input)
    {
        return new VectorModification<float>() { Value = input, Operation = DefaultOperator };
    }
    public VectorModification<float> InputToIntermediate(VectorModification<float> input)
    {
        return input;
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