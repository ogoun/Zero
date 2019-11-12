using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ZeroLevel.Services;

namespace TestApp
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var dict = new Dictionary<string, int>();
            var methods = dict.GetType().GetMethods(BindingFlags.Static |
                                         BindingFlags.Instance |
                                         BindingFlags.Public |
                                         BindingFlags.NonPublic |
                                         BindingFlags.FlattenHierarchy)
                .Select(s => s.Name).OrderBy(s => s);

            var fiber = new Fiber();
            fiber
                .Add((s) => { Console.WriteLine("1"); s.Add<int>("1", 1); return s; })
                .Add((s) => { Console.WriteLine("2"); s.Add<int>("2", 2); return s; })
                .Add((s) => { Console.WriteLine("3"); s.Add<int>("3", 3); return s; })
                .Add((s) => { Console.WriteLine("4"); s.Add<int>("4", 4); return s; })
                .Add((s) => { Console.WriteLine("5"); s.Add<int>("5", 5); return s; });

            var result = fiber.Run();
            Console.WriteLine();
            Console.WriteLine("Result");
            foreach (var key in result.Keys<int>())
            {
                Console.WriteLine($"{key}: {result.Get<int>(key)}");
            }

            Console.ReadKey();


            /*Configuration.Save(Configuration.ReadFromApplicationConfig());
            Bootstrap.Startup<MyService>(args,
                () => Configuration.ReadSetFromIniFile("config.ini"))
                .EnableConsoleLog(ZeroLevel.Services.Logging.LogLevel.System | ZeroLevel.Services.Logging.LogLevel.FullDebug)
                //.UseDiscovery()
                .Run()
                .WaitWhileStatus(ZeroServiceStatus.Running)
                .Stop();
            Bootstrap.Shutdown();*/
        }
    }
}