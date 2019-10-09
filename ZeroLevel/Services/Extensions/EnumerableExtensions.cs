using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ZeroLevel
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> Safe<T>(this IEnumerable<T> collection)
        {
            return collection ?? Enumerable.Empty<T>();
        }

        public static bool Contains<T>(this IEnumerable<T> collection, Predicate<T> condition)
        {
            return collection.Any(x => condition(x));
        }

        public static bool IsEmpty<T>(this IEnumerable<T> collection)
        {
            if (collection == null)
                return true;
            var coll = collection as ICollection;
            if (coll != null)
                return coll.Count == 0;
            return !collection.Any();
        }

        public static bool IsNotEmpty<T>(this IEnumerable<T> collection)
        {
            return !IsEmpty(collection);
        }

        public static IEnumerable<T> Batch<T>(this IEnumerator<T> source, int size)
        {
            yield return source.Current;
            for (var i = 1; i < size && source.MoveNext(); i++)
            {
                yield return source.Current;
            }
        }

        public static IEnumerable<IEnumerable<T>> Chunkify<T>(this IEnumerable<T> source, int size)
        {
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    yield return Batch(enumerator, size);
                }
            }
        }
    }
}