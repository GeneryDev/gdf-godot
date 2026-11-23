namespace GDF.Attributes;

public class Attribute<T> : Attribute<T, T, SimpleModifierChain<T>>
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

    public Attribute() {}

    public Attribute(T baseValue)
    {
        BaseValue = baseValue;
    }

    protected override T GetInitialParam()
    {
        return BaseValue;
    }
}

public class SimpleModifierChain<T> : ModifierChain<T>
{
    public float WeightThreshold = 1.0f;

    protected override void Reduce(ref Step current, in Step incoming, float weight)
    {
        if (weight >= WeightThreshold)
            current = incoming;
    }
}