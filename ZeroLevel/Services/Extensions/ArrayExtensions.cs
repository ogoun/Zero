﻿using System;
using System.Linq;

namespace ZeroLevel
{
    public static class ArrayExtensions
    {
        /// <summary>
        /// Глубокое копирование массива
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
        /// Проверяет вхождение одного массива в другой
        /// </summary>
        /// <typeparam name="T">Тиа элементов массивов</typeparam>
        /// <returns>true - массив содержит указанный подмассив</returns>
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

        static bool IsEmptyLocate<T>(T[] array, T[] candidate)
        {
            return array == null
                || candidate == null
                || array.Length == 0
                || candidate.Length == 0
                || candidate.Length > array.Length;
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
            return Array.Equals(first, second);
        }
    }
}
