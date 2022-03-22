using System;
using System.Collections.Generic;
using System.Linq;

namespace ZeroLevel
{
    public static class ArrayExtensions
    {
        /// <summary>
        /// Deep copy array
        /// </summary>
        public static T[] DeepCopy<T>(this T[] array)
            where T : ICloneable
        {
            return array.Select(a => (T)a.Clone()).ToArray();
        }

        // Copyright (c) 2008-2013 Hafthor Stefansson
        // Distributed under the MIT/X11 software license
        // Ref: http://www.opensource.org/licenses/mit-license.php.
        public static unsafe bool UnsafeEquals(byte[] first, byte[] second)
        {
            if (null == first && null == second)
                return true;
            if (null == first || null == second)
                return false;
            if (ReferenceEquals(first, second))
                return true;
            if (first.Length != second.Length) return false;
            fixed (byte* p1 = first, p2 = second)
            {
                byte* x1 = p1, x2 = p2;
                int l = first.Length;
                for (int i = 0; i < l / 8; i++, x1 += 8, x2 += 8)
                    if (*((long*)x1) != *((long*)x2)) return false;
                if ((l & 4) != 0) { if (*((int*)x1) != *((int*)x2)) return false; x1 += 4; x2 += 4; }
                if ((l & 2) != 0) { if (*((short*)x1) != *((short*)x2)) return false; x1 += 2; x2 += 2; }
                if ((l & 1) != 0) if (*((byte*)x1) != *((byte*)x2)) return false;
                return true;
            }
        }

        /// <summary>
        /// Checks whether one array is in another
        /// </summary>
        public static bool Contains<T>(this T[] array, T[] candidate)
        {
            if (IsEmptyLocate(array, candidate))
                return false;

            for (int a = 0; a < array.Length; a++)
            {
                if (array[a].Equals(candidate[0]))
                {
                    int i = 1;
                    for (; i < candidate.Length && (a + i) < array.Length; i++)
                    {
                        if (false == array[a + i].Equals(candidate[i]))
                            break;
                    }
                    if (i == candidate.Length)
                        return true;
                }
            }
            return false;
        }

        private static bool IsEmptyLocate<T>(this T[] array, T[] candidate)
        {
            return array == null
                || candidate == null
                || array.Length == 0
                || candidate.Length == 0
                || candidate.Length > array.Length;
        }

        public static IEnumerable<T> GetRow<T>(this T[,] array, int row)
        {
            for (int i = 0; i < array.GetLength(1); i++)
            {
                yield return array[row, i];
            }
        }

        public static IEnumerable<T> GetColumn<T>(this T[,] array, int column)
        {
            for (int i = 0; i < array.GetLength(0); i++)
            {
                yield return array[i, column];
            }
        }

        public static bool Equals(byte[] first, byte[] second)
        {
            if (null == first && null == second)
                return true;
            if (null == first || null == second)
                return false;
            if (ReferenceEquals(first, second))
                return true;
            if (first.Length != second.Length) return false;
            for (int i = 0; i < first.Length; i++)
            {
                if (first[i] != second[i]) return false;
            }
            return true;
        }
    }
}