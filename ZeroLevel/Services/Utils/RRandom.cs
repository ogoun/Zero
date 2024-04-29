using System;
using System.Security.Cryptography;

namespace ZeroLevel.Services.Utils
{
    public static class RRandom
    {
        public static int RandomInteger(int min, int max)
        {
            uint scale = uint.MaxValue;
            while (scale == uint.MaxValue)
            {
                byte[] four_bytes = new byte[4];
                RandomNumberGenerator.Fill(four_bytes);
                scale = BitConverter.ToUInt32(four_bytes, 0);
            }
            return (int)(min + (max - min) * (scale / (double)uint.MaxValue));
        }

        public static bool OverLimit(int limit)
        {
            return RandomInteger(0, 1000) > limit;
        }
    }
}
