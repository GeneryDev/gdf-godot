namespace GDF.Attributes;

public class Attribute<T> : Attribute<T, T>
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

    public float WeightThreshold = 1.0f;
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

    protected override Modification Reduce(in Modification current, in Modification incoming, float weight)
    {
        return weight < WeightThreshold ? current : incoming;
    }
}

public static class SimpleAttributeExtensions
{
    public static void Set<T>(this Attribute<T> target, AttributeModifierToken token, T value)
    {
        target.Modify(token, value);
    }
    public static AttributeModifierTokenTyped<TAttr> Set<TAttr, TValue>(this AttributeModifierTokenTyped<TAttr> token, TValue value)
        where TAttr : Attribute<TValue>
    {
        return token.Modify(value);
    }
}