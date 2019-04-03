using System;
using System.Collections.Generic;

namespace ZeroLevel
{
    public static class LinqExtension
    {
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>
            (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            if (source != null)
            {
                var seenKeys = new HashSet<TKey>();
                foreach (TSource element in source)
                {
                    if (seenKeys.Add(keySelector(element)))
                    {
                        yield return element;
                    }
                }
            }
        }
    }
}