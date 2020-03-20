using Newtonsoft.Json;
using System;
using ZeroLevel;
using ZeroLevel.Logging;

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
            Configuration.Save(Configuration.ReadFromApplicationConfig());
            Bootstrap.Startup<MyService>(args,
                () => Configuration.ReadSetFromIniFile("config.ini"))
                .EnableConsoleLog(LogLevel.System | LogLevel.FullDebug)
                //.UseDiscovery()
                .Run()
                .WaitWhileStatus(ZeroServiceStatus.Running)
                .Stop();
            Bootstrap.Shutdown();
        }
    }
}