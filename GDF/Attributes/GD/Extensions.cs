using Godot;

namespace GDF.Attributes;

public static class AttributeSetModifierExtensions
{
    public static AttributeSetModifier<TKey> BindToNode<TKey>(this AttributeSetModifier<TKey> set, Node node)
    {
        if (node != null)
        {
            var nodeName = node.Name;
            set.SetIntegrityChecker((ref IntegrityCheckParams param) =>
            {
                if (GodotObject.IsInstanceValid(node))
                {
                    return new IntegrityCheckResult()
                    {
                        Passed = true
                    };
                }
                else
                {
                    if (param.RemoveFailures)
                        set.Clear();
                    return new IntegrityCheckResult()
                    {
                        Passed = false,
                        ResultMessage = $"Attribute set modifier is now stale due to bound node (formerly '{nodeName}') having been freed."
                    };
                }
            });
        }
        else
        {
            set.SetIntegrityChecker(null);
        }
        return set;
    }
}