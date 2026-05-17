using System.Collections.Generic;
using GDF.Util;
using Godot;
using Godot.Collections;

namespace GDF.Composition;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/component_index.png")]
public partial class ComponentIndex : Node
{
    [Export]
    public Node ComponentOwner
    {
        get => GetParent();
        // ReSharper disable once ValueParameterNotUsed
        set {}
    }

    [Export]
    public Node[] OtherComponentContainers = System.Array.Empty<Node>();
    
    public T GetComponent<T>() where T : class
    {
        if (ComponentOwner?.GetChildOfType<T>() is { } siblingComp) return siblingComp;
        if (OtherComponentContainers != null)
        {
            foreach (var container in OtherComponentContainers)
            {
                if (container?.GetChildOfType<T>() is { } otherContainerComp) return otherContainerComp;
            }
        }
        return null;
    }
    public List<T> GetComponents<T>(List<T> output) where T : class
    {
        if (ComponentOwner is { } parent)
        {
            foreach (var sibling in parent.IterateChildrenOfType<T>())
            {
                output.Add(sibling);
            }
        }
        if (OtherComponentContainers != null)
        {
            foreach (var container in OtherComponentContainers)
            {
                if (container == null) continue;
                foreach (var child in container.IterateChildrenOfType<T>())
                {
                    output.Add(child);
                }
            }
        }
        return output;
    }


#if TOOLS
    public override void _ValidateProperty(Dictionary property)
    {
        var propName = property["name"].AsStringName();
        var usage = property["usage"].As<PropertyUsageFlags>();

        if (propName == PropertyName.ComponentOwner)
        {
            usage |= PropertyUsageFlags.ReadOnly;
        }

        property["usage"] = Variant.From(usage);
    }
#endif
}