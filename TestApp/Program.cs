using Newtonsoft.Json;
using System;
using System.Net;
using System.Threading;
using ZeroLevel;
using ZeroLevel.Logging;
using ZeroLevel.Network;
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

        static void SimpleCSTest()
        {
            var server_router = new Router();
            server_router.RegisterInbox<string>("test", (c, line) =>
            {
                Console.WriteLine(line);
            });

            server_router.RegisterInbox<string, string>("req", (c, line) =>
            {
                Console.WriteLine($"Request: {line}");
                return line.ToUpperInvariant();
            });

            var server = new SocketServer(new System.Net.IPEndPoint(IPAddress.Any, 666), server_router);


            var client_router = new Router();
            var client = new SocketClient(new IPEndPoint(IPAddress.Loopback, 666), client_router);

            var frm = FrameFactory.Create("req", MessageSerializer.SerializeCompatible("Hello world"));

            while (Console.KeyAvailable == false)
            {
                client.Request(frm, data =>
                {
                    var line = MessageSerializer.DeserializeCompatible<string>(data);
                    Console.WriteLine($"Response: {line}");
                });
                Thread.Sleep(2000);
            }
        }

        public static double[] Generate(int vector_size)
        {
            var rnd = new Random((int)Environment.TickCount);
            var vector = new double[vector_size];
            for (int i = 0; i < vector_size; i++)
            {
                vector[i] = 50.0d - rnd.NextDouble() * 100.0d;
            }
            return vector;
        }
    }
}