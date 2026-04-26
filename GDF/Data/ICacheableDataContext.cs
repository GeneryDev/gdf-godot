using System;
using System.Collections.Generic;

namespace GDF.Data;

public interface ICacheableDataContext<in T> : IDataContext where T : struct, IDataContext, ICacheableDataContext<T>
{
    public bool EqualsContext(T otherCtx);

    public bool CanCache();

    private static readonly List<(T structContext, WeakReference<IDataContext> boxedContext)> Cache = new();
    
    public static IDataContext Boxed(T cacheable)
    {
        if (!cacheable.CanCache()) return cacheable;
        
        for (var index = 0; index < Cache.Count; index++)
        {
            var (structContext, boxedContext) = Cache[index];
            if (cacheable.EqualsContext(structContext))
            {
                if (boxedContext.TryGetTarget(out var cachedBoxed))
                {
                    return cachedBoxed;
                }
                else
                {
                    IDataContext replacementBoxed = cacheable;
                    Cache[index] = (structContext, new WeakReference<IDataContext>(replacementBoxed));
                    return replacementBoxed;
                }
            }
        }
        
        // not in cache, add
        IDataContext newBoxed = cacheable;
        Cache.Add((cacheable, new WeakReference<IDataContext>(newBoxed)));
        return newBoxed;
    }
}

public static class CacheableDataContexts
{
    public static IDataContext Boxed<T>(this T cacheable) where T : struct, ICacheableDataContext<T>
    {
        return ICacheableDataContext<T>.Boxed(cacheable);
    }
}