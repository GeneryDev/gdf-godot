using System.Collections.Generic;
using GDF.Util;
using Godot;

namespace GDF.Components;

[GlobalClass]
public partial class ComponentOwner : Node
{
    [Export]
    public Node[] OtherComponentContainers;
    
    public T GetComponent<T>() where T : class
    {
        if (GetParent()?.GetChildOfType<T>() is { } siblingComp) return siblingComp;
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
        if (GetParent() is { } parent)
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
}