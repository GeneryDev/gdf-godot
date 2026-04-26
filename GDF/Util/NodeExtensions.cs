using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace GDF.Util;

public static class NodeExtensions
{
    public static T GetChildOfType<T>(this Node node)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is T t) return t;
        }

        return default;
    }
    
    public static Array<T> GetChildrenOfType<[MustBeVariant]T>(this Node node)
    {
        var nodeArr = node.GetChildren();
        var tArr = new Array<T>();
        int count = nodeArr.Count;
        for (var i = 0; i < count; i++)
        {
            if(nodeArr[i] is T t) tArr.Add(t);
        }

        return tArr;
    }
    
    public static List<T> ListChildrenOfType<T>(this Node node)
    {
        var nodeArr = node.GetChildren();
        var tArr = new List<T>();
        int count = nodeArr.Count;
        for (var i = 0; i < count; i++)
        {
            if(nodeArr[i] is T t) tArr.Add(t);
        }

        return tArr;
    }
    
    public static IEnumerable<T> IterateChildrenOfType<T>(this Node node)
    {
        var nodeArr = node.GetChildren();
        for (var i = 0; i < nodeArr.Count; i++)
        {
            if (nodeArr[i] is T t)
            {
                yield return t;
            }
        }

        yield break;
    }
    
    public static Node GetChildOfTypeOrScriptName<T>(this Node node) where T : Node
    {
        foreach (var child in node.GetChildren())
        {
            if (child is T t) return t;
            if (Engine.IsEditorHint() && child.GetScript().As<Script>() is { } script && script.ResourcePath.EndsWith($"/{typeof(T).Name}.cs"))
            {
                return child;
            }
        }

        return null;
    }
    
    public static Array<Node> GetChildrenOfTypeOrScriptName<[MustBeVariant]T>(this Node node) where T : Node
    {
        var nodeArr = node.GetChildren();
        var tArr = new Array<Node>();
        int count = nodeArr.Count;
        for (var i = 0; i < count; i++)
        {
            var child = nodeArr[i];
            if (child is T) tArr.Add(child);
            if (Engine.IsEditorHint() && child.GetScript().As<Script>() is { } script && script.ResourcePath.EndsWith($"/{typeof(T).Name}.cs"))
            {
                tArr.Add(child);
            }
        }

        return tArr;
    }

    /// <summary>
    /// Calculates the global transform of the Node3D, regardless of whether the node is inside the tree.
    /// If the node is inside the tree, returns GlobalTransform.
    /// Otherwise, it calculates the transform based on all of the ancestors' local transforms, up to the root.
    /// </summary>
    /// <param name="node">The node whose transform should be calculated</param>
    /// <returns>The global transform of the node up to the root</returns>
    public static Transform3D GetGlobalTransformTreeless(this Node3D node)
    {
        if (node.IsInsideTree()) return node.GlobalTransform;
        var transform = node.Transform;
        if (node.GetParentNode3D() is { } parent)
        {
            transform = parent.GetGlobalTransformTreeless() * transform;
        }

        return transform;
    }

    public static string GetSceneAndPathString(this Node node)
    {
        if (node == null) return "";
        var owner = node?.Owner;
        return $"{owner?.SceneFilePath}:{owner?.GetPathTo(node) ?? (node.IsInsideTree() ? node.GetPath().ToString() : node.Name.ToString())}";
    }
}