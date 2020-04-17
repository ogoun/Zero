using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using ZeroLevel;
using ZeroLevel.Implementation.Semantic.Helpers;
using ZeroLevel.Logging;
using ZeroLevel.Services.Serialization;
using ZeroLevel.Services.Trees;

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