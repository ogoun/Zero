using System.Collections.Generic;

namespace System.Linq
{
    public static class LinqExtension
    {
        public static IEnumerable<T[]> ZipLongest<T>(this IEnumerable<T> left, IEnumerable<T> right)
        {
            IEnumerator<T> leftEnumerator = left.GetEnumerator();
            IEnumerator<T> rightEnumerator = right.GetEnumerator();

            bool hasLeft = leftEnumerator.MoveNext();
            bool hasRight = rightEnumerator.MoveNext();

            while (hasLeft || hasRight)
            {
                if (hasLeft && hasRight)
                {
                    yield return new T[] { leftEnumerator.Current, rightEnumerator.Current };
                }
                else if (hasLeft)
                {
                    yield return new T[] { leftEnumerator.Current, default(T) };
                }
                else if (hasRight)
                {
                    yield return new T[] { default(T), rightEnumerator.Current };
                }

                hasLeft = leftEnumerator.MoveNext();
                hasRight = rightEnumerator.MoveNext();
            }
        }

        /*public static IEnumerable<TSource> DistinctBy<TSource, TKey>
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
        }*/

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
            T[] arr = new T[size];
            int index = 0;
            foreach (var obj in source)
            {
                arr[index] = obj;
                index++;
                if (index >= size)
                {
                    yield return arr;
                    index = 0;
                    arr = new T[size];
                }
            }
            if (index > 0)
            {
                var narr = new T[index];
                Array.Copy(arr, narr, index);
                yield return narr;
            }
        }
    }
}