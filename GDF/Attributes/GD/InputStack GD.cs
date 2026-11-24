using Godot;

// ReSharper disable once CheckNamespace
namespace GDF.Attributes.Impl;

public partial class InputStack : IAttributeComputable<Variant>
{
    Variant IAttributeComputable<Variant>.CurrentValue => CurrentValue.Id;
}