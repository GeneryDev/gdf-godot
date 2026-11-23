using System;
using Godot;

namespace GDF.Attributes.Impl;

public partial class FloatAttribute :
    StandardAttribute<float, FloatModifierChain>,
    ITypedStandardAttribute<float, FloatModifierChain>
{
    public int InterpolationMode
    {
        get => ModifierChain.InterpolationMode;
        set
        {
            if (ModifierChain.InterpolationMode == value) return;
            ModifierChain.InterpolationMode = value;
            MarkDirty();
        }
    }

    public FloatAttribute() {}
    public FloatAttribute(float baseValue) : base(baseValue) {}
    
    public abstract class Functions : FloatModifierChain.Functions;
}
public partial class FloatModifierChain : StandardModifierChain<float>,
    ITypedStandardModifierChain<float>,
    IModifiable<int, ModificationOperation<float>>,
    IModifiable<Mod.Set<int>, ModificationOperation<float>>,
    IModifiable<Mod.Add<float>, ModificationOperation<float>>,
    IModifiable<Mod.Multiply<float>, ModificationOperation<float>>,
    IModifiable<Mod.Add<int>, ModificationOperation<float>>,
    IModifiable<Mod.Multiply<int>, ModificationOperation<float>>
{
    private int _interpolationMode = InterpolationModeLinear;
    public const int InterpolationModeLinear = 0;
    public const int InterpolationModeRadians = 1;
    public const int InterpolationModeDegrees = 2;

    public int InterpolationMode
    {
        get => _interpolationMode;
        set
        {
            if (_interpolationMode == value) return;
            _interpolationMode = value;
            MarkDirty();
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

    ModificationOperation<float> IModifiable<int, ModificationOperation<float>>.InToParam(int value)
    {
        return new ModificationOperation<float>(value);
    }

    ModificationOperation<float> IModifiable<Mod.Set<int>, ModificationOperation<float>>.InToParam(Mod.Set<int> value)
    {
        return new ModificationOperation<float>(Functions.Set, value.Operand);
    }

    ModificationOperation<float> IModifiable<Mod.Add<float>, ModificationOperation<float>>.InToParam(Mod.Add<float> value)
    {
        return new ModificationOperation<float>(Functions.Add, value.Operand);
    }

    ModificationOperation<float> IModifiable<Mod.Multiply<float>, ModificationOperation<float>>.InToParam(Mod.Multiply<float> value)
    {
        return new ModificationOperation<float>(Functions.Multiply, value.Operand);
    }

    ModificationOperation<float> IModifiable<Mod.Add<int>, ModificationOperation<float>>.InToParam(Mod.Add<int> value)
    {
        return new ModificationOperation<float>(Functions.Add, value.Operand);
    }

    ModificationOperation<float> IModifiable<Mod.Multiply<int>, ModificationOperation<float>>.InToParam(Mod.Multiply<int> value)
    {
        return new ModificationOperation<float>(Functions.Multiply, value.Operand);
    }

    public float Compute(in int initial)
    {
        return this.Compute((float)initial);
    }

    public class Functions
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

public partial class BoolAttribute :
    StandardAttribute<bool, BoolModifierChain>,
    ITypedStandardAttribute<bool, BoolModifierChain>
{
    public BoolAttribute() {}
    public BoolAttribute(bool baseValue) : base(baseValue) {}

    public abstract class Functions : BoolModifierChain.Functions;
}
public partial class BoolModifierChain : StandardModifierChain<bool>,
    ITypedStandardModifierChain<bool>,
    IModifiable<Mod.And<bool>, ModificationOperation<bool>>,
    IModifiable<Mod.Or<bool>, ModificationOperation<bool>>
{
    protected override bool Lerp(bool a, bool b, float weight)
    {
        return weight < 1 ? a : b;
    }

    ModificationOperation<bool> IModifiable<Mod.And<bool>, ModificationOperation<bool>>.InToParam(Mod.And<bool> value)
    {
        return new ModificationOperation<bool>(Functions.And, value.Operand);
    }

    ModificationOperation<bool> IModifiable<Mod.Or<bool>, ModificationOperation<bool>>.InToParam(Mod.Or<bool> value)
    {
        return new ModificationOperation<bool>(Functions.Or, value.Operand);
    }

    public abstract class Functions
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

public partial class IntAttribute :
    StandardAttribute<int, IntModifierChain>,
    ITypedStandardAttribute<int, IntModifierChain>
{
    public IntAttribute() {}
    public IntAttribute(int baseValue) : base(baseValue) {}
    
    public abstract class Functions : IntModifierChain.Functions;
}
public partial class IntModifierChain: StandardModifierChain<int>,
    ITypedStandardModifierChain<int>,
    IModifiable<Mod.Add<int>, ModificationOperation<int>>,
    IModifiable<Mod.Multiply<int>, ModificationOperation<int>>,
    IModifiable<Mod.And<int>, ModificationOperation<int>>,
    IModifiable<Mod.Or<int>, ModificationOperation<int>>
{
    protected override int Lerp(int a, int b, float weight)
    {
        return (int)(a + (b - a) * weight);
    }
    
    ModificationOperation<int> IModifiable<Mod.Add<int>, ModificationOperation<int>>.InToParam(Mod.Add<int> value)
    {
        return new ModificationOperation<int>(Functions.Add, value.Operand);
    }

    ModificationOperation<int> IModifiable<Mod.Multiply<int>, ModificationOperation<int>>.InToParam(Mod.Multiply<int> value)
    {
        return new ModificationOperation<int>(Functions.Multiply, value.Operand);
    }

    ModificationOperation<int> IModifiable<Mod.And<int>, ModificationOperation<int>>.InToParam(Mod.And<int> value)
    {
        return new ModificationOperation<int>(Functions.And, value.Operand);
    }

    ModificationOperation<int> IModifiable<Mod.Or<int>, ModificationOperation<int>>.InToParam(Mod.Or<int> value)
    {
        return new ModificationOperation<int>(Functions.Or, value.Operand);
    }
    
    public class Functions
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
