using System;
using ZeroLevel;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.AddConsoleLogger();
            var ex = Bootstrap.CreateExchange();

            var port = ReadPort();

            var server = ex.UseHost(port);
            server.RegisterInbox<string, string>("time", (c, s) => { Log.Info($"Request time: [{s}]"); return DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"); });
            server.RegisterInbox<string, string>("whois", (c, s) => { Log.Info($"Request whois: [{s}]"); return $"[{Environment.MachineName}] {Environment.UserDomainName}\\{Environment.UserName}"; });

            server.OnConnect += Server_OnConnect;
            server.OnDisconnect += Server_OnDisconnect;
            Console.ReadKey();
        }

        private static void Server_OnDisconnect(ZeroLevel.Network.ISocketClient obj)
        {
            Log.Info($"Client disconnected: {obj.Endpoint.Address}:{obj.Endpoint.Port}");
        }

        private static void Server_OnConnect(ZeroLevel.Network.IClient obj)
        {
            Log.Info($"Client connected: {obj.Endpoint.Address}:{obj.Endpoint.Port}");
        }

        static int ReadPort()
        {
            int port = -1;
            do
            {
                Console.WriteLine("PORT>");
                var port_line = Console.ReadLine();
                if (int.TryParse(port_line, out port) == false || port <= 0)
                {
                    port = -1;
                    Console.WriteLine($"Incorrect port '{port_line}'");
                }
            } while (port <= 0);
            return port;
        }
    }
}
