using System;

namespace ZeroLevel.Services.Network
{
    public static class NetworkStreamFastObfuscator
    {
        public static byte[] PrepareData(byte[] data)
        {
            var packet = new byte[data.Length + 6];
            packet[0] = 181;
            Array.Copy(BitConverter.GetBytes(data.Length), 0, packet, 1, 4);
            packet[5] = (byte)(packet[0] ^ packet[1] ^ packet[2] ^ packet[3] ^ packet[4]);
            HashData(data, packet[5]);
            Array.Copy(data, 0, packet, 6, data.Length);
            return packet;
        }

        public static void HashData(byte[] data, byte initialmask)
        {
            if (data.Length == 0) return;
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

        public static void DeHashData(byte[] data, byte initialmask)
        {
            if (data.Length == 0) return;
            for (var i = data.Length - 1; i > 0; i--)
            {
                data[i] ^= data[i - 1];
            }
            data[0] ^= initialmask;
        }
    }
}
