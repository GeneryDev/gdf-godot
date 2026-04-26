using System.Collections.Generic;

namespace GDF.Util;

public interface ITagged<in T>
{
    public bool HasTag(T tag);
}

public static class TaggedExtensions
{
    public static bool TagsAnyOf<T>(this ITagged<T> tagged, IEnumerable<T> tags)
    {
        if (tags == null) return true;
        foreach (var tag in tags)
            if (tagged.HasTag(tag))
                return true;
        return false;
    }

    public static bool TagsAllOf<T>(this ITagged<T> tagged, IEnumerable<T> tags)
    {
        if (tags == null) return true;
        foreach (var tag in tags)
            if (!tagged.HasTag(tag))
                return false;
        return true;
    }

    public static bool TagsNoneOf<T>(this ITagged<T> tagged, IEnumerable<T> tags)
    {
        if (tags == null) return true;
        foreach (var tag in tags)
            if (tagged.HasTag(tag))
                return false;
        return true;
    }
}