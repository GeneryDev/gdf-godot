#if TOOLS
using GDF.Util;
using Godot;

namespace GDF.Editor;

public partial class EditorUtils
{
    public delegate void NodeAndPropertySelectedEventHandler(NodePath nodePath, string propertyPath);
    public delegate void NodeSelectedEventHandler(NodePath nodePath);


    public static void ShowNodeAndPropertyPicker(Node source, NodeAndPropertySelectedEventHandler callback)
    {
        ShowNodeAndPropertyPicker(source, source, callback);
    }

    public static void ShowNodeAndPropertyPicker(Node source, Node currentNode, NodeAndPropertySelectedEventHandler callback)
    {
        if (source == null) return;
        
        EditorInterface.Singleton.PopupNodeSelector(Callable.From((NodePath nodePath) =>
        {
            if (nodePath is not { IsEmpty: false }) return;
        
            var node = (source.Owner ?? source).GetNode(nodePath);

            if (node == null)
            {
                GD.PrintErr("Selected node path does not point to a node? Cannot continue method selection.");
                return;
            }

            var relPath = source.GetPathTo(node);
            
            EditorInterface.Singleton.PopupPropertySelector(node, Callable.From((NodePath propertyPath) =>
            {
                if (propertyPath.IsNullOrEmpty()) return;
                string property = propertyPath.GetConcatenatedSubNames();
                if (string.IsNullOrEmpty(property)) return;
                callback?.Invoke(relPath, property);
            }));
        }), currentValue: currentNode);
    }

    public static void ShowNodePicker(Node source, Node currentNode, NodeSelectedEventHandler callback)
    {
        if (source == null) return;
        
        EditorInterface.Singleton.PopupNodeSelector(Callable.From((NodePath nodePath) =>
        {
            if (nodePath is not { IsEmpty: false }) return;
        
            var node = (source.Owner ?? source).GetNode(nodePath);

            if (node == null)
            {
                GD.PrintErr("Selected node path does not point to a node? Cannot continue method selection.");
                return;
            }

            var relPath = source.GetPathTo(node);
            
            callback?.Invoke(relPath);
        }), currentValue: currentNode);
    }
}
#endif