using System;
using GDF.PropertyStacks.Internal;
using Godot;

namespace GDF.PropertyStacks.Definitions;

[GlobalClass]
[Tool]
public partial class NodeProperty : PropertyDefinitionResource, IPropertyDefinition<Node, Node, Empty>
{
    public Node GetInitialValue(Empty cache)
    {
        return null;
    }

    public Node InputToIntermediate(object input)
    {
        return input switch
        {
            Node node => node,
            Variant v => v.As<Node>(),
            _ => throw new ArgumentOutOfRangeException(nameof(input), input,
                $"Provided type {input?.GetType().Name} is not supported in node property")
        };
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