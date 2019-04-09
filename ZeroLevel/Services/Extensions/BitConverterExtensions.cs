using System;
using System.Collections.Generic;

namespace ZeroLevel.Services.Extensions
{
    public static class BitConverterExt
    {
        public static byte[] GetBytes(this Decimal dec)
        {
            var bites = decimal.GetBits(dec);
            var bytes = new List<byte>();
            foreach (var i in bites)
            {
                bytes.AddRange(BitConverter.GetBytes(i));
            }
            return bytes.ToArray();
        }

        public static decimal ToDecimal(this int[] parts)
        {
            bool sign = (parts[3] & 0x80000000) != 0;
            byte scale = (byte)((parts[3] >> 16) & 0x7F);
            return new Decimal(parts[0], parts[1], parts[2], sign, scale);
        }
    }
}