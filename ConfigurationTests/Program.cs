using System;
using System.Collections.Generic;
using System.Linq;
using ZeroLevel;

namespace ConfigurationTests
{
    class Program
    {

        static void Main(string[] args)
        {
            var list = new List<int>();
            for (int i = 0; i < 100; i++)
            {
                list.Add(i);
            }
            var collection = list.Chunkify(6).ToList();
            foreach (var t in collection)
            {
                Console.WriteLine(string.Join("; ", t.Select(n => n.ToString("D2"))));
            }
            Console.ReadKey();
            return;


            var config = Configuration.ReadFromIniFile("config.ini").Bind<AppConfig>();
            Console.WriteLine(config.Url);
            Console.WriteLine(config.BatchSize);
            Console.WriteLine("Ports");
            foreach (var port in config.Port)
            {
                Console.WriteLine($"\t{port}");
            }
            Console.WriteLine("Shemes");
            foreach (var sheme in config.Sheme)
            {
                Console.WriteLine($"\t{sheme}");
            }
            Console.ReadKey();
        }
    }
}
