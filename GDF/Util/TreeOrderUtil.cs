using System.Collections.Generic;
using Godot;

namespace GDF.Util;

public static class TreeOrderUtil
{
    public static int CompareTreeOrder(Node a, Node b)
    {
        if (a == b) return 0;
        if (!GodotObject.IsInstanceValid(a) || !GodotObject.IsInstanceValid(b)) return 0;
        return b.IsGreaterThan(a) ? 1 : -1;
    }

    public static void InsertInTreeOrder<T>(List<T> list, T node) where T : Node
    {
        if (list.Count == 0 || node == default || !node.IsInsideTree() || CompareTreeOrder(list[^1], node) > 0)
        {
            list.Add(node);
            return;
        }

        var minIndex = 0; // inclusive
        int maxIndex = list.Count; // exclusive

        while (true)
        {
            if (maxIndex == minIndex)
            {
                list.Insert(maxIndex, node);
                return;
            }

            int pivotIndex = (minIndex + maxIndex) / 2;

            var pivotEntry = list[pivotIndex];
            switch (CompareTreeOrder(pivotEntry, node))
            {
                case -1:
                {
                    maxIndex = pivotIndex;
                    continue;
                }
                case 0:
                {
                    list.Insert(pivotIndex + 1, node);
                    return;
                }
                case 1:
                {
                    minIndex = pivotIndex + 1;
                    continue;
                }
            }
        }
    }
}