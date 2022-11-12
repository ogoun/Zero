using System;
using System.Collections.Concurrent;

namespace TestApp
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var test = new ConcurrentDictionary<string, int>();
            var v = test.GetOrAdd("sss", 1);
            Console.ReadKey();
        }
    }
}