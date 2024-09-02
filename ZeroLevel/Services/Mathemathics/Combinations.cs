using System.Collections.Generic;

namespace ZeroLevel.Services.Mathemathics
{
    public static class Combinations
    {
        private static IEnumerable<int[]> GenerateIndiciesForUniqueSets(int m, int n)
        {
            int[] result = new int[m];
            Stack<int> stack = new Stack<int>(m);
            stack.Push(0);
            while (stack.Count > 0)
            {
                int index = stack.Count - 1;
                int value = stack.Pop();
                while (value < n)
                {
                    result[index++] = value++;
                    stack.Push(value);
                    if (index != m) continue;
                    yield return result;
                    break;
                }
            }
        }

        public static IEnumerable<T[]> GenerateUniqueSets<T>(T[] original, int k)
        {
            T[] result = new T[k];
            foreach (var indices in GenerateIndiciesForUniqueSets(k, original.Length))
            {
                for (int i = 0; i < k; i++)
                {
                    result[i] = original[indices[i]];
                }
                yield return result;
            }
        }
    }
}
