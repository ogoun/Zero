using System;
using System.Collections.Concurrent;
using ZeroLevel.Services.Network.Utils;

namespace TestApp
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine(Network.ExternalIP);
        }
    }
}