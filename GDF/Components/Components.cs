using System.Collections.Generic;
using GDF.Util;
using Godot;

namespace GDF.Components;

public static class Components
{
    public static T GetComponent<T>(this Node node) where T : class
    {
        if (node == null) return null;
        if (node.GetChildOfType<ComponentOwner>() is { } componentOwner)
        {
            return componentOwner.GetComponent<T>();
        }
        else
        {
            if (node.GetChildOfType<T>() is { } directChild) return directChild;
            if (node.Owner?.GetChildOfType<ComponentOwner>() is { } ownerComponents)
            {
                return ownerComponents.GetComponent<T>();
            }
        }

        return null;
    }

    public static List<T> GetComponents<T>(this Node node) where T : class
    {
        return node.GetComponents(new List<T>());
    }

    public static List<T> GetComponents<T>(this Node node, List<T> output) where T : class
    {
        if (node == null) return output;
        if (node.GetChildOfType<ComponentOwner>() is { } componentOwner)
        {
            componentOwner.GetComponents<T>(output);
        }
        else
        {
            foreach (var child in node.IterateChildrenOfType<T>())
            {
                output.Add(child);
            }
            if (node.Owner?.GetChildOfType<ComponentOwner>() is { } ownerComponents)
            {
                ownerComponents.GetComponents<T>(output);
            }
        }
        return output;
    }
}