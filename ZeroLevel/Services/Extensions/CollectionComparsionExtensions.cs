using System;
using System.Collections.Generic;
using System.Linq;

namespace ZeroLevel
{
    public static partial class CollectionComparsionExtensions
    {
        private sealed class SimpleComparer<T> : IEqualityComparer<T>
        {
            private readonly Func<T, T, bool> _comparer;

            public SimpleComparer()
            {
                _comparer = (a, b) => object.Equals(a, b);
            }

            public SimpleComparer(Func<T, T, bool> comparer)
            {
                _comparer = comparer;
            }

            public bool Equals(T x, T y)
            {
                return _comparer(x, y);
            }

            public int GetHashCode(T obj)
            {
                return obj.GetHashCode();
            }
        }

        /// <summary>
        /// Checks for the same content of a collection of strings, including sorted in different ways
        /// </summary>
        public static bool StringEnumerableEquals(this IEnumerable<string> A, IEnumerable<string> B)
        {
            if (A == null && B == null) return true;
            if (A == null || B == null) return false;
            return A.Count() == B.Count() && A.Intersect(B).Count() == B.Count();
        }

        /// <summary>
        /// Checks for the same content of collections, including sorted in different ways
        /// </summary>
        public static bool NoOrderingEquals<T>(this IEnumerable<T> A, IEnumerable<T> B)
        {
            if (A == null && B == null) return true;
            if (A == null || B == null) return false;
            return A.Count() == B.Count() && A.Intersect(B, new SimpleComparer<T>()).Count() == B.Count();
        }

        public static bool NoOrderingEquals<T>(this IEnumerable<T> A, IEnumerable<T> B, Func<T, T, bool> comparer)
        {
            if (A == null && B == null) return true;
            if (A == null || B == null) return false;
            return A.Count() == B.Count() && A.Intersect(B, new SimpleComparer<T>(comparer)).Count() == B.Count();
        }

        /// <summary>
        /// Checks for the same content collections
        /// </summary>
        public static bool OrderingEquals<T>(this IEnumerable<T> A, IEnumerable<T> B)
        {
            if (A == null && B == null) return true;
            if (A == null || B == null) return false;
            if (A.Count() != B.Count()) return false;
            var enumA = A.GetEnumerator();
            var enumB = B.GetEnumerator();
            while (enumA.MoveNext() && enumB.MoveNext())
            {
                if (enumA.Current == null && enumB.Current == null) continue;
                if (enumA.Current == null || enumB.Current == null) return false;
                if (enumA.Current.Equals(enumB.Current) == false) return false;
            }
            return true;
        }

        public static bool OrderingEquals<T>(this IEnumerable<T> A, IEnumerable<T> B, Func<T, T, bool> comparer)
        {
            if (A == null && B == null) return true;
            if (A == null || B == null) return false;
            if (A.Count() != B.Count()) return false;
            var enumA = A.GetEnumerator();
            var enumB = B.GetEnumerator();
            while (enumA.MoveNext() && enumB.MoveNext())
            {
                if (enumA.Current == null && enumB.Current == null) continue;
                if (enumA.Current == null || enumB.Current == null) return false;
                if (comparer(enumA.Current, enumB.Current) == false) return false;
            }
            return true;
        }

        /// <summary>
        /// Calculate hash for collection
        /// </summary>
        public static int GetEnumHashCode<T>(this IEnumerable<T> A)
        {
            int hc = 0;
            if (A != null)
            {
                foreach (var p in A)
                    hc ^= p.GetHashCode();
            }
            return hc;
        }
    }
}