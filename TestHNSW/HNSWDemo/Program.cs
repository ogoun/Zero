using HNSWDemo.Tests;
using System;

namespace HNSWDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            new AutoClusteringMNISTTest().Run();
            //new HistogramTest().Run();
            Console.WriteLine("Completed");
            Console.ReadKey();
        }
    }
}
