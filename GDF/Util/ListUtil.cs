using System.Collections.Generic;

namespace GDF.Util;

public static class ListUtil
{
    public static void RemoveDuplicates<T>(this List<T> list) where T : class
    {
        if (list == null) return;
        for (var i = 0; i < list.Count; i++)
        {
            for (int j = i + 1; j < list.Count; j++)
            {
                if (list[i] == list[j])
                {
                    // duplicates
                    list.RemoveAt(j);
                    j--;
                    continue;
                }
            }
        }
    }
    public static void RemoveNulls<T>(this List<T> list) where T : class
    {
        if (list == null) return;
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i] == null)
            {
                i--;
                list.RemoveAt(i);
                continue;
            }
        }
    }
}