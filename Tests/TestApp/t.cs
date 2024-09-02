using System;
using System.Linq;

namespace TestApp
{
    internal class t
    {
        public int Calculate()
        {
            var c = int.Parse(Console.ReadLine());
            var data = Console.ReadLine().Split(' ').Select(s => int.Parse(s)).ToArray();
            int result = 0;
            for (int i = 0; i < c - 1; i++)
            {
                for (int j = i + 1; j < c; j++)
                {
                    var d1 = Math.Min(data[i], data[j]);
                    var d2 = Math.Max(data[i], data[j]);
                    if (d2 > 100000) continue;
                    if ((d1 != 0 && d2 != 0) && ((float)d1 / d2) >= .9f)
                        result++;
                }
            }
            return result;
        }
    }
}
