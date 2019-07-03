using System;
using System.Net;
using ZeroLevel;

namespace TestApp
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Bootstrap.Startup<MyService>(args,
                () => Configuration.ReadSetFromIniFile("config.ini"))
                //.ReadServiceInfo()
                //.UseDiscovery()
                .Run()
                .WaitWhileStatus(ZeroServiceStatus.Running)
                .Stop();
            Bootstrap.Shutdown();
        }
    }
}
