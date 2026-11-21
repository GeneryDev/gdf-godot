using System;

namespace GDF.Attributes.Impl;

public partial class FloatAttribute : StandardAttribute<float>,
    ITypedStandardAttribute<float>,
    IAttributeModifiable<int, ModificationOperation<float>>,
    IAttributeModifiable<Mod.Set<int>, ModificationOperation<float>>,
    IAttributeModifiable<Mod.Add<float>, ModificationOperation<float>>,
    IAttributeModifiable<Mod.Multiply<float>, ModificationOperation<float>>,
    IAttributeModifiable<Mod.Add<int>, ModificationOperation<float>>,
    IAttributeModifiable<Mod.Multiply<int>, ModificationOperation<float>>
{
    public const int InterpolationModeLinear = 0;
    public const int InterpolationModeRadians = 1;
    public const int InterpolationModeDegrees = 2;

    public int InterpolationMode = InterpolationModeLinear;

    public FloatAttribute() {}
    public FloatAttribute(float baseValue) : base(baseValue) {}

    protected override float Lerp(float a, float b, float weight)
    {
        return InterpolationMode switch
        {
            InterpolationModeRadians => Mathf.LerpAngle(a, b, weight),
            InterpolationModeDegrees => Mathf.RadToDeg(Mathf.LerpAngle(Mathf.DegToRad(a), Mathf.DegToRad(b), weight)),
            _ => Mathf.Lerp(a, b, weight)
        };
    }

    ModificationOperation<float> IAttributeModifiable<int, ModificationOperation<float>>.InToParam(int value)
    {
        return new ModificationOperation<float>(value);
    }

    ModificationOperation<float> IAttributeModifiable<Impl.Mod.Set<int>, ModificationOperation<float>>.InToParam(Impl.Mod.Set<int> value)
    {
        return new ModificationOperation<float>(Functions.Set, value.Operand);
    }

    ModificationOperation<float> IAttributeModifiable<Impl.Mod.Add<float>, ModificationOperation<float>>.InToParam(Impl.Mod.Add<float> value)
    {
        return new ModificationOperation<float>(Functions.Add, value.Operand);
    }

    ModificationOperation<float> IAttributeModifiable<Impl.Mod.Multiply<float>, ModificationOperation<float>>.InToParam(Impl.Mod.Multiply<float> value)
    {
        return new ModificationOperation<float>(Functions.Multiply, value.Operand);
    }

    ModificationOperation<float> IAttributeModifiable<Impl.Mod.Add<int>, ModificationOperation<float>>.InToParam(Impl.Mod.Add<int> value)
    {
        return new ModificationOperation<float>(Functions.Add, value.Operand);
    }

    ModificationOperation<float> IAttributeModifiable<Impl.Mod.Multiply<int>, ModificationOperation<float>>.InToParam(Impl.Mod.Multiply<int> value)
    {
        return new ModificationOperation<float>(Functions.Multiply, value.Operand);
    }

    public static class Functions
    {
        public static readonly Func<float, float, float> Set = (_, incoming) => incoming;
        public static readonly Func<float, float, float> Add = (current, incoming) => current + incoming;
        public static readonly Func<float, float, float> Multiply = (current, incoming) => current * incoming;

        public static Func<float, float, float> FromName(string name)
        {
            return name switch
            {
                "Set" => Set,
                "Add" => Add,
                "Multiply" => Multiply,
                _ => null
            };
        }
    }

    private static class Mathf
    {
        // Copied from Godot Mathf
        
        public static float AngleDifference(float from, float to)
        {
            float num = (float) (((double) to - (double) from) % 6.2831854820251465);
            return (float) (2.0 * (double) num % 6.2831854820251465) - num;
        }
        public static float LerpAngle(float from, float to, float weight)
        {
            return from + AngleDifference(from, to) * weight;
        }
        
        public static float DegToRad(float deg) => deg * ((float) Math.PI / 180f);
        public static float RadToDeg(float rad) => rad * 57.29578f;
        
        public static float Lerp(float from, float to, float weight) => from + (to - from) * weight;
    }
}

public partial class BoolAttribute : StandardAttribute<bool>,
    ITypedStandardAttribute<bool>,
    IAttributeModifiable<Mod.And<bool>, ModificationOperation<bool>>,
    IAttributeModifiable<Mod.Or<bool>, ModificationOperation<bool>>
{
    public BoolAttribute() {}
    public BoolAttribute(bool baseValue) : base(baseValue) {}

    protected override bool Lerp(bool a, bool b, float weight)
    {
        return weight < 1 ? a : b;
    }

    ModificationOperation<bool> IAttributeModifiable<Impl.Mod.And<bool>, ModificationOperation<bool>>.InToParam(Impl.Mod.And<bool> value)
    {
        return new ModificationOperation<bool>(Functions.And, value.Operand);
    }

    ModificationOperation<bool> IAttributeModifiable<Impl.Mod.Or<bool>, ModificationOperation<bool>>.InToParam(Impl.Mod.Or<bool> value)
    {
        return new ModificationOperation<bool>(Functions.Or, value.Operand);
    }

    public static class Functions
    {
        public static readonly Func<bool, bool, bool> Set = (_, incoming) => incoming;
        public static readonly Func<bool, bool, bool> And = (current, incoming) => current && incoming;
        public static readonly Func<bool, bool, bool> Or = (current, incoming) => current || incoming;

        public static Func<bool, bool, bool> FromName(string name)
        {
            return name switch
            {
                "Set" => Set,
                "And" => And,
                "Or" => Or,
                _ => null
            };
        }
    }
}

public partial class IntAttribute: StandardAttribute<int>,
    ITypedStandardAttribute<int>,
    IAttributeModifiable<Mod.Add<int>, ModificationOperation<int>>,
    IAttributeModifiable<Mod.Multiply<int>, ModificationOperation<int>>,
    IAttributeModifiable<Mod.And<int>, ModificationOperation<int>>,
    IAttributeModifiable<Mod.Or<int>, ModificationOperation<int>>
{
    public IntAttribute() {}
    public IntAttribute(int baseValue) : base(baseValue) {}
    
    protected override int Lerp(int a, int b, float weight)
    {
        return (int)(a + (b - a) * weight);
    }
    
    ModificationOperation<int> IAttributeModifiable<Impl.Mod.Add<int>, ModificationOperation<int>>.InToParam(Impl.Mod.Add<int> value)
    {
        return new ModificationOperation<int>(Functions.Add, value.Operand);
    }

    ModificationOperation<int> IAttributeModifiable<Impl.Mod.Multiply<int>, ModificationOperation<int>>.InToParam(Impl.Mod.Multiply<int> value)
    {
        return new ModificationOperation<int>(Functions.Multiply, value.Operand);
    }

    ModificationOperation<int> IAttributeModifiable<Impl.Mod.And<int>, ModificationOperation<int>>.InToParam(Impl.Mod.And<int> value)
    {
        return new ModificationOperation<int>(Functions.And, value.Operand);
    }

    ModificationOperation<int> IAttributeModifiable<Impl.Mod.Or<int>, ModificationOperation<int>>.InToParam(Impl.Mod.Or<int> value)
    {
        return new ModificationOperation<int>(Functions.Or, value.Operand);
    }
    
    public static class Functions
    {
        public static readonly Func<int, int, int> Set = (_, incoming) => incoming;
        public static readonly Func<int, int, int> Add = (current, incoming) => current + incoming;
        public static readonly Func<int, int, int> Multiply = (current, incoming) => current * incoming;
        public static readonly Func<int, int, int> And = (current, incoming) => current & incoming;
        public static readonly Func<int, int, int> Or = (current, incoming) => current | incoming;

        public static Func<int, int, int> FromName(string name)
        {
            return name switch
            {
                "Set" => Set,
                "Add" => Add,
                "Multiply" => Multiply,
                "And" => And,
                "Or" => Or,
                _ => null
            };
        }
    }
}
