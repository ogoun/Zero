using HNSWDemo.Tests;
using System;
using System.IO;
using ZeroLevel.HNSW;

namespace HNSWDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            //new QuantizatorTest().Run();
            //new AutoClusteringMNISTTest().Run();
            new AccuracityTest().Run();
            Console.WriteLine("Completed");
            Console.ReadKey();
        }

        static int GetC(string file)
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var index = name.IndexOf("_M");
            if (index > 0)
            {
                index = name.IndexOf("_", index + 2);
                if (index > 0)
                {
                    var num = name.Substring(index + 1, name.Length - index - 1);
                    return int.Parse(num);
                }
            }
            return -1;
        }
    }
}
