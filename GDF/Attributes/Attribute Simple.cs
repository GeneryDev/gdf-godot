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

    protected override void Reduce(ref Modification current, in Modification incoming, float weight)
    {
        if (weight >= WeightThreshold)
            current = incoming;
    }
}