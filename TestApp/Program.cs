using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ZeroLevel.Services.Web;

namespace TestApp
{
    public class TestQuery
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public string[] Roles { get; set; }
    }

    internal static class Program
    {
        private static string Serialize(object instance)
        {
            return JsonConvert.SerializeObject(instance);
        }

        private static void Main(string[] args)
        {
            var t = new TestQuery
            {
                Age = 133,
                Roles = new[] { "ad\"\"\"min", "user", "operator" },
                Name = "su"
            };
            var builder = new UrlBuilder(Serialize);
            var url = builder.BuildRequestUrlFromDTO("http://google.com", "/api/v0/getuserinfo", t);
            Console.WriteLine(url);
            Console.ReadKey();

            /*var fiber = new Fiber();
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
            }*/




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