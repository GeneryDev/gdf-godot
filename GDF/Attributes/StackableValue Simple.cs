namespace GDF.Attributes;

public class StackableValue<T> : StackableValue<T, T>
{
    public override T CurrentValue => GetComputed().Param;

    public T BaseValue;
    public float WeightThreshold = 1.0f;

    public StackableValue() {}

    public StackableValue(T baseValue)
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

public static class SimpleStackableValueExtensions
{
    public static void Set<T>(this StackableValue<T, T> target, StackableValueToken token, T value)
    {
        target.Modify(token, value);
    }
    public static StackableValueTokenTyped<T, T> Set<T>(this StackableValueTokenTyped<T, T> token, T value)
    {
        return token.Modify(value);
    }
}