using System;

namespace ZeroLevel.Services.Utils
{
    public static class Timestamp
    {
        /// <summary>
        /// Current unix timestamp in ms
        /// </summary>
        public static long UtcNow => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        /// <summary>
        /// Unix timestamp in ms
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static long FromDateTimeOffset(DateTimeOffset offset) => offset.ToUnixTimeMilliseconds();
        public static long UtcNowAddDays(int days) => DateTimeOffset.UtcNow.AddDays(days).ToUnixTimeMilliseconds();
        public static long UtcNowAddSeconds(int seconds) => DateTimeOffset.UtcNow.AddSeconds(seconds).ToUnixTimeMilliseconds();
        public static long Max => DateTimeOffset.MaxValue.ToUnixTimeMilliseconds();
        public static long Min => DateTimeOffset.MinValue.ToUnixTimeMilliseconds();
        public static DateTimeOffset ToDateTimeOffsest(long timeStamp) => DateTimeOffset.FromUnixTimeMilliseconds(timeStamp);
    }
}
