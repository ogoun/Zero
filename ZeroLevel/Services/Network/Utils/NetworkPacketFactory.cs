using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ZeroLevel.Network
{
    public static class NetworkPacketFactory
    {
        public const byte MAGIC = 153;
        public const byte MAGIC_REQUEST = 155;
        public const byte MAGIC_RESPONSE = 185;
        public const byte MAGIC_KEEP_ALIVE = 187;

        private static int _current_request_id = 0;

        private static byte[] _keep_alive = new byte[] { 187, 0, 0, 0, 4, 128, 64, 32, 42 };
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] KeepAliveMessage() => _keep_alive;

        public static byte[] Message(byte[] data)
        {
            var packet = new byte[data.Length + 6];
            packet[0] = MAGIC;
            Array.Copy(BitConverter.GetBytes(data.Length), 0, packet, 1, 4);
            packet[5] = (byte)(MAGIC ^ packet[1] ^ packet[2] ^ packet[3] ^ packet[4]);
            HashData(data, packet[5]);
            Array.Copy(data, 0, packet, 6, data.Length);
            return packet;
        }

        public static byte[] Reqeust(byte[] data, out int requestId)
        {
            var packet = new byte[data.Length + 6 + 4];
            packet[0] = (MAGIC | MAGIC_REQUEST);
            Array.Copy(BitConverter.GetBytes(data.Length), 0, packet, 1, 4);            

            requestId = Interlocked.Increment(ref _current_request_id);
            var id = BitConverter.GetBytes(requestId);
            packet[5] = id[0];
            packet[6] = id[1];
            packet[7] = id[2];
            packet[8] = id[3];

            packet[9] = (byte)(MAGIC ^ packet[1] ^ packet[2] ^ packet[3] ^ packet[4]);

            HashData(data, packet[9]);
            Array.Copy(data, 0, packet, 10, data.Length);
            return packet;
        }

        public static byte[] Response(byte[] data, int requestId)
        {
            var packet = new byte[data.Length + 6 + 4];
            packet[0] = (MAGIC | MAGIC_RESPONSE);
            Array.Copy(BitConverter.GetBytes(data.Length), 0, packet, 1, 4);            

            var id = BitConverter.GetBytes(requestId);
            packet[5] = id[0];
            packet[6] = id[1];
            packet[7] = id[2];
            packet[8] = id[3];

            packet[9] = (byte)(MAGIC ^ packet[1] ^ packet[2] ^ packet[3] ^ packet[4]);

            HashData(data, packet[9]);
            Array.Copy(data, 0, packet, 10, data.Length);
            return packet;
        }

        private static void HashData(byte[] data, byte initialmask)
        {
            if (data == null || data.Length == 0) return;
            int i = 1;
            data[0] ^= initialmask;
            for (; i < (data.Length - 8); i += 8)
            {
                data[i + 0] ^= data[i - 1];
                data[i + 1] ^= data[i + 0];
                data[i + 2] ^= data[i + 1];
                data[i + 3] ^= data[i + 2];
                data[i + 4] ^= data[i + 3];
                data[i + 5] ^= data[i + 4];
                data[i + 6] ^= data[i + 5];
                data[i + 7] ^= data[i + 6];
            }
            for (; i < data.Length; i++)
            {
                data[i] ^= data[i - 1];
            }
        }
    }
}
