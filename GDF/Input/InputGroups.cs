using GDF.Attributes.Impl;

namespace GDF.Input;

public partial class InputGroups
{
    public readonly InputStack Default = new();
    public readonly InputStack Mouse = new();
}