using Godot;

namespace GDF.Attributes;

public class VariantAttribute<[MustBeVariant] T> : Attribute<T, T, SimpleVariantModifierChain<T>>, IModifiable<Variant>
{
    public T BaseValue
    {
        get => _baseValue;
        set
        {
            _baseValue = value;
            MarkDirty();
        }
    }

    public override T CurrentValue => GetComputed().Param;

    private T _baseValue;

    public VariantAttribute() {}

    public VariantAttribute(T baseValue)
    {
        BaseValue = baseValue;
    }

    protected override T GetInitialParam()
    {
        return BaseValue;
    }
    
    public void Modify(ModifierToken token, Variant value)
    {
        this.Modify(token, value.As<T>());
    }

    public static implicit operator T(VariantAttribute<T> attribute)
    {
        return attribute.CurrentValue;
    }
}

public class SimpleVariantModifierChain<[MustBeVariant] T> : ModifierChain<T>, IModifiable<Variant>
{
    public float WeightThreshold = 1.0f;

    protected override void Reduce(ref Step current, in Step incoming, float weight)
    {
        if (weight >= WeightThreshold)
            current = incoming;
    }

    public void Modify(ModifierToken token, Variant value)
    {
        this.Modify(token, value.As<T>());
    }
}