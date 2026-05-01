using GDF.PropertyStacks.Internal;
using Godot;

namespace GDF.PropertyStacks.Definitions;

[GlobalClass]
[Tool]
public partial class NodeListProperty : VariantListProperty<Node, Empty>,
    IPropertyAcceptsInput<Node, VariantListEntry<Node, Empty>>
{
    public VariantListEntry<Node, Empty> InputToIntermediate(Node input)
    {
        return VariantListEntry.Create<Node, Empty>(input, 0);
    }
}