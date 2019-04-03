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

        public static decimal ToDecimal(this byte[] bytes)
        {
            var arr = new int[4];
            for (int i = 0; i < 15; i += 4)
            {
                arr[i % 4] = BitConverter.ToInt32(bytes, i);
            }
            return new Decimal(arr);
        }
    }
}