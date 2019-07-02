using System;
using System.Net;
using ZeroLevel;

namespace TestApp
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var se = Bootstrap.Startup<MyService>(args,
                () => Configuration.ReadSetFromIniFile("config.ini"))
                .ReadServiceInfo()
                //.UseDiscovery()
                .Run();

            var router = se.Service.UseHost(8800);
            router.RegisterInbox<string, string>("upper", (c, s) => s.ToUpperInvariant());

            var client = se.Service.ConnectToService(new IPEndPoint(IPAddress.Loopback, 8800));
            client.Request<string, string>("upper", "hello", s => Console.WriteLine(s));

            se.WaitWhileStatus(ZeroServiceStatus.Running)
            .Stop();
        }
    }
}
