using System;
using ZeroLevel;

namespace ConfigurationTests
{
    class Program
    {
        
        static void Main(string[] args)
        {
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
