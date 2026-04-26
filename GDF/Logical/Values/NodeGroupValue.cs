using System.Collections.Generic;
using Godot;

namespace GDF.Logical.Values;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/node_group.png")]
public partial class NodeGroupValue : ValueSource, IEnumerableValueSource<Node>, IEnumerableValueSource<Variant>
{
    [Export] public StringName GroupName;
    
    public override Variant GetValue(Node source)
    {
        return source.GetTree()?.GetFirstNodeInGroup(GroupName) ?? default;
    }

    IEnumerable<Variant> IEnumerableValueSource<Variant>.EnumerateValues(Node source)
    {
        var tree = source.GetTree();
        if(tree == null) yield break;
        foreach (var node in tree.GetNodesInGroup(GroupName))
        {
            yield return node;
        }
    }

    IEnumerable<Node> IEnumerableValueSource<Node>.EnumerateValues(Node source)
    {
        var tree = source.GetTree();
        if(tree == null) yield break;
        foreach (var node in tree.GetNodesInGroup(GroupName))
        {
            yield return node;
        }
    }
}