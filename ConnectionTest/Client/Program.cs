using System;
using System.Net;
using System.Threading;
using ZeroLevel;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.AddConsoleLogger();
            var ex = Bootstrap.CreateExchange();

            var address = ReadIP();
            var port = ReadPort();
            var client = ex.GetConnection(new IPEndPoint(address, port));
            Console.WriteLine("Esc - exit\r\nEnter - recreate connection\r\nSpace - send request");
            long index = 0;
            while (true)
            {
                if (Console.KeyAvailable)
                {
                    switch (Console.ReadKey().Key)
                    {
                        case ConsoleKey.Escape:
                            client?.Dispose();
                            return;
                        case ConsoleKey.Enter:
                            address = ReadIP();
                            port = ReadPort();
                            try
                            {
                                if (client != null)
                                {
                                    client.Dispose();
                                }

                                client = ex.GetConnection(new IPEndPoint(address, port));
                            }
                            catch (Exception exc)
                            {
                                Log.Error(exc, "Fault recreate connection");
                            }
                            break;
                        case ConsoleKey.Spacebar:
                            Log.Info("Send request");
                            if (client == null)
                            {
                                client = ex.GetConnection(new IPEndPoint(address, port));
                                if (client == null)
                                {
                                    Log.Info("No connection");
                                }
                                continue;
                            }
                            if (false == client.Request<string, string>("time", "Time reqeust", s => { Log.Info($"Got time response '{s}'"); }))
                            {
                                Log.Warning("Send time request fault");
                            }
                            break;
                    }
                }
                Thread.Sleep(100);
                index++;
                if (index % 50 == 0)
                {
                    if (client == null)
                    {
                        client = ex.GetConnection(new IPEndPoint(address, port));
                        if (client == null)
                        {
                            Log.Info("No connection");
                        }
                        continue;
                    }
                    if (false == client.Request<string, string>("whois", "Whois reqeust", s => { Log.Info($"Got whois response '{s}'"); }))
                    {
                        Log.Warning("Send whois request fault");
                    }
                }
            }
        }

        static IPAddress ReadIP()
        {
            IPAddress ip = null;
            do
            {
                Console.WriteLine("IP>");
                var address = Console.ReadLine();
                if (IPAddress.TryParse(address, out ip) == false)
                {
                    ip = null;
                    Console.WriteLine($"Incorrect ip address '{address}'");
                }
            } while (ip == null);
            return ip;
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
