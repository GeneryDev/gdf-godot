using System;
using GDF.PropertyStacks.Internal;
using Godot;

namespace GDF.PropertyStacks.Definitions;

[GlobalClass]
[Tool]
public partial class NodeProperty : PropertyDefinitionResource,
    IPropertyDefinition<Node, Node, Empty>,
    IPropertyAcceptsInput<Node, Node>,
    IPropertyAcceptsInput<Variant, Node>
{
    public Node GetInitialValue(Empty cache)
    {
        return null;
    }

    public Node InputToIntermediate(Node input)
    {
        return input;
    }

    public Node InputToIntermediate(Variant input)
    {
        return input.As<Node>();
    }

    public Node Reduce(Node lower, Node higher, float weight,
        PropertyFrameHandle handle)
    {
        return weight < 1 ? lower : higher;
    }

    public Node IntermediateToOutput(Node value)
    {
        return value;
    }

    public Variant OutputToVariant(Node value)
    {
        return Variant.From(value);
    }

    public override IProperty CreateProperty()
    {
        return new PropertyImpl<Node, Node, Empty>(this);
    }

    public Empty CreateCache()
    {
        return new Empty();
    }
}