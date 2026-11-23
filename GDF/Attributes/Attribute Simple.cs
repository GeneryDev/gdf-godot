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