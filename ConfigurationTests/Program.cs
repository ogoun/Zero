using System;
using ZeroLevel;

namespace ConfigurationTests
{
    class Program
    {

        static void Main(string[] args)
        {
            var config = Configuration.ReadSetFromIniFile("config.ini").Bind<AppConfig>();
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
            Console.WriteLine($"Range list: {string.Join(", ", config.List)}");

            Console.WriteLine("Service");
            Console.WriteLine($"\tAppKey: {config.Service.AppKey}");
            Console.WriteLine($"\tAppName: {config.Service.AppName}");
            Console.WriteLine($"\tServiceGroup: {config.Service.ServiceGroup}");
            Console.WriteLine($"\tServiceType: {config.Service.ServiceType}");
            
            Console.ReadKey();
        }
    }
}

