using Godot;

namespace GDF.Logical.Values;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/node_value.png")]
public partial class ValueSourceNode : Node, IValueSource
{
    [Export]
    public ValueSource ValueSource;

    public Variant GetValue() => GetValue(null);
    public T GetValue<[MustBeVariant]T>() => GetValue<T>(null);

    public Variant GetValue(Node source)
    {
        return ValueSource?.GetValue(this) ?? default;
    }

    public T GetValue<[MustBeVariant]T>(Node source)
    {
        return ValueSource != null ? ValueSource.GetValue<T>(this) : default;
    }
}