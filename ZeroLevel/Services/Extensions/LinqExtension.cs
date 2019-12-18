using System.Collections.Generic;

namespace System.Linq
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

        public static IList<TSource> Materialize<TSource>(this IEnumerable<TSource> source)
        {
            if (source is IList<TSource>)
            {
                // Already a list, use it as is
                return (IList<TSource>)source;
            }
            else
            {
                // Not a list, materialize it to a list
                return source.ToList();
            }
        }
        public static IEnumerable<IEnumerable<T>> Chunkify<T>(this IEnumerable<T> source, int size)
        {
            if (source == null)
            {
                yield break;
            }
            if (size <= 0)
            {
                throw new ArgumentException("chunkSize must be greater than 0.");
            }
            while (source.Any())
            {
                yield return source.Take(size);
                source = source.Skip(size);
            }
        }
    }
}