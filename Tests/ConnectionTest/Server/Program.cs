using System;
using System.Collections.Generic;
using ZeroLevel;
using ZeroLevel.Services.HashFunctions;
using ZeroLevel.Services.Serialization;

namespace Server
{
    //netsh advfirewall firewall add rule name="ClientTest" dir=in action=allow protocol=TCP localport=5016
    public class Info
        : IBinarySerializable
    {
        public uint Id;
        public uint Length;
        public uint Checksum;

        public void Deserialize(IBinaryReader reader)
        {
            this.Id = reader.ReadUInt32();
            this.Length = reader.ReadUInt32();
            this.Checksum = reader.ReadUInt32();
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteUInt32(this.Id);
            writer.WriteUInt32(this.Length);
            writer.WriteUInt32(this.Checksum);
        }
    }

    public class Fragment
        : IBinarySerializable
    {
        public uint Id;
        public uint Offset;
        public uint Checksum;
        public byte[] Payload;

        public void Deserialize(IBinaryReader reader)
        {
            this.Id = reader.ReadUInt32();
            this.Offset = reader.ReadUInt32();
            this.Checksum = reader.ReadUInt32();
            this.Payload = reader.ReadBytes();
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteUInt32(this.Id);
            writer.WriteUInt32(this.Offset);
            writer.WriteUInt32(this.Checksum);
            writer.WriteBytes(this.Payload);
        }
    }

    public class Data
    {
        public uint Checksum;
        public uint Length;
        public uint ActualLength;
        public byte[] Payload;
    }

    class Program
    {
        private readonly static Dictionary<uint, Data> _incoming = new Dictionary<uint, Data>();
        private readonly static XXHashUnsafe _hash = new XXHashUnsafe(667);

        static void Main(string[] args)
        {
            Log.AddConsoleLogger(ZeroLevel.Logging.LogLevel.FullDebug | ZeroLevel.Logging.LogLevel.System);
            var ex = Bootstrap.CreateExchange();
            var port = ReadPort();
            var server = ex.UseHost(port);

            server.RegisterInbox<string, string>("time", (c, s) => { Log.Info($"Request time: [{s}]"); return DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"); });
            server.RegisterInbox<string, string>("whois", (c, s) => { Log.Info($"Request whois: [{s}]"); return $"[{Environment.MachineName}] {Environment.UserDomainName}\\{Environment.UserName}"; });

            server.RegisterInbox<Info, bool>("start", (c, i) =>
            {
                Start(i);
                return true;
            });

            server.RegisterInbox<Fragment, bool>("part", (c, p) =>
            {
                return WriteFragment(p);
            });

            server.RegisterInbox<uint>("complete", (c, id) => Complete(id));

            Log.Warning("Started");

            server.OnConnect += Server_OnConnect;
            server.OnDisconnect += Server_OnDisconnect;
            Console.ReadKey();
        }

        private static void Start(Info info)
        {
            var data = new Data
            {
                Checksum = info.Checksum,
                ActualLength = 0,
                Length = info.Length,
                Payload = new byte[info.Length]
            };
            _incoming.Add(info.Id, data);
            Log.Info($"Start incoming data id '{info.Id}'. {info.Length} bytes. Checksum: {info.Checksum}");
        }

        private static bool WriteFragment(Fragment fragment)
        {
            var checksum = _hash.Hash(fragment.Payload);
            if (checksum != fragment.Checksum)
            {
                Log.Warning($"[WriteFragment] Wrong checksum (checksum: {checksum} expected: {fragment.Checksum})! ID: '{fragment.Id}'. Offset: '{fragment.Offset}'. Length: '{fragment.Payload.Length}' bytes.");
                return false;
            }
            if (!_incoming.ContainsKey(fragment.Id))
            {
                Log.Warning($"[WriteFragment] Data ID: '{fragment.Id}' not found. Offset: '{fragment.Offset}'. Length: '{fragment.Payload.Length}' bytes.");
                return false;
            }
            Array.Copy(fragment.Payload, 0, _incoming[fragment.Id].Payload, fragment.Offset, fragment.Payload.Length);
            return true;
        }

        private static void Complete(uint id)
        {
            if (_incoming.ContainsKey(id))
            {
                var checksum = _hash.Hash(_incoming[id].Payload);
                if (checksum != _incoming[id].Checksum)
                {
                    Log.Warning($"[Complete] Wrong checksum (checksum: {checksum} expected: {_incoming[id].Checksum})! ID: '{id}'");
                }
                else
                {
                    Log.Info($"Data '{id}' successfully received");
                }
                _incoming.Remove(id);
            }
            else
            {
                Log.Warning($"[Complete] Data ID '{id}' not found");
            }
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
