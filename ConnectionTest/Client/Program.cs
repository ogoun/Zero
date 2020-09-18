using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ZeroLevel;
using ZeroLevel.Network;
using ZeroLevel.Services.HashFunctions;
using ZeroLevel.Services.Serialization;

namespace Client
{
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

    class Program
    {
        private readonly static XXHashUnsafe _hash = new XXHashUnsafe(667);

        static void Main(string[] args)
        {
            Log.AddConsoleLogger();
            var ex = Bootstrap.CreateExchange();
            var address = ReadIP();
            var port = ReadPort();
            ex.RoutesStorage.Set("server", new IPEndPoint(address, port));

            uint index = 0;
            while (true)
            {
                if (Console.KeyAvailable)
                {
                    switch (Console.ReadKey().Key)
                    {
                        case ConsoleKey.Escape:
                            ex?.Dispose();
                            return;
                    }
                }
                if (index % 2 == 0)
                {
                    SendDataEqParts(ex, index, 1024 + index * 3 + 1);
                }
                else
                {
                    SendDataDiffParts(ex.GetConnection("server"), index, 1024 + index * 3 + 1);
                }
                index++;
            }
        }

        static void SendDataDiffParts(IClient client, uint id, uint length)
        {
            var payload = GetByteArray(length);
            var full_checksum = _hash.Hash(payload);
            var info = new Info { Checksum = full_checksum, Id = id, Length = length };
            if (client.Request<Info, bool>("start", info, res =>
            {
                Log.Info($"Success start sending packet '{id}'");
            }))
            {
                uint size = 1;
                uint offset = 0;
                while (offset < payload.Length)
                {
                    var fragment = GetFragment(id, payload, offset, size);
                    if (!client.Request<Fragment, bool>("part", fragment, res =>
                    {
                        if (!res)
                        {
                            Log.Info($"Fault server incoming packet '{id}' fragment. Offset: '{offset}'. Size: '{size}' bytes.");
                        }
                    }))
                    {
                        Log.Warning($"Can't start send packet '{id}' fragment. Offset: '{offset}'. Size: '{size}' bytes. No connection");
                    }
                    offset += size;
                    size += 1;
                }
                client.Send<uint>("complete", id);
            }
            else
            {
                Log.Warning($"Can't start send packet '{id}'. No connection");
            }
        }

        static void SendDataEqParts(IExchange exchange, uint id, uint length)
        {
            var payload = GetByteArray(length);
            var full_checksum = _hash.Hash(payload);
            var info = new Info { Checksum = full_checksum, Id = id, Length = length };
            if (exchange.Request<Info, bool>("server", "start", info))
            {
                Log.Info($"Success start sending packet '{id}'");
                uint size = 4096;
                uint offset = 0;
                while (offset < payload.Length)
                {
                    var fragment = GetFragment(id, payload, offset, size);
                    if (!exchange.Request<Fragment, bool>("server", "part", fragment))
                    {
                            Log.Info($"Fault server incoming packet '{id}' fragment. Offset: '{offset}'. Size: '{size}' bytes.");
                    }
                    offset += size;
                }
                exchange.Send<uint>("server", "complete", id);
            }
            else
            {
                Log.Warning($"Can't start send packet '{id}'. No connection");
            }
        }

        private static Fragment GetFragment(uint id, byte[] data, uint offset, uint size)
        {
            int diff = (int)(-(data.Length - (offset + size)));
            if (diff > 0)
            {
                size -= (uint)diff;
            }
            var payload = new byte[size];
            Array.Copy(data, offset, payload, 0, size);
            var ch = _hash.Hash(payload);
            return new Fragment
            {
                Id = id,
                Checksum = ch,
                Offset = offset,
                Payload = payload
            };
        }

        private static byte[] GetByteArray(uint size)
        {
            Random rnd = new Random();
            Byte[] b = new Byte[size];
            rnd.NextBytes(b);
            return b;
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
