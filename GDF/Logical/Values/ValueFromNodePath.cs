using Godot;

namespace GDF.Logical.Values;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/node_value.png")]
public partial class ValueFromNodePath : ValueSource
{
    [Export]
    public NodePath NodePath;

    [Export] public bool ErrorOnMissingNode = true;
    [Export] public bool EvaluateNestedSourceNodes = true;

    public ValueFromNodePath()
    {
    }

    public ValueFromNodePath(NodePath nodePath)
    {
        NodePath = nodePath;
    }

    public Variant Get(Node source)
    {
        if (source == null)
        {
            GD.PrintErr($"Attempted to retrieve node '{NodePath}' via a {nameof(ValueFromNodePath)} value source, but parameter '{nameof(source)}' is null");
            return default;
        }
        var referencedNode = ErrorOnMissingNode ? source.GetNode(NodePath) : source.GetNodeOrNull(NodePath);
        if(EvaluateNestedSourceNodes && referencedNode is ValueSourceNode newSourceNode) return newSourceNode.GetValue(source);
        return referencedNode;
    }
    public T Get<[MustBeVariant]T>(Node source) where T : Node
    {
        if (source == null) return default;
        var referencedNode = ErrorOnMissingNode ? source.GetNode(NodePath) : source.GetNodeOrNull(NodePath);
        if(EvaluateNestedSourceNodes && referencedNode is ValueSourceNode newSourceNode) return newSourceNode.GetValue<T>(source);
        return referencedNode as T;
    }

    public override Variant GetValue(Node source)
    {
        return Get(source);
    }

    public override string ToString()
    {
        return NodePath ?? "<null>";
    }
}