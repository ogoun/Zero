using System;
using System.Globalization;
using System.Threading;

namespace ZeroLevel.Services
{
    /// <summary>
    /// Provides various options for generating identifiers
    /// </summary>
    public static class IdGenerator
    {
        /// <summary>
        /// Returns a function to get consecutive int64 values.
        /// </summary>
        public static Func<long> IncreasingSequenceIdGenerator()
        {
            long id = 0;
            return new Func<long>(() => Interlocked.Increment(ref id));
        }
        /// <summary>
        /// Creates a base64 hash from the specified datetime
        /// </summary>
        public static string HashFromDateTime(DateTime date)
        {
            var bytes = BitConverter.GetBytes(date.Ticks);
            return Convert.ToBase64String(bytes)
                                    .Replace('+', '_')
                                    .Replace('/', '-')
                                    .TrimEnd('=');
        }
        /// <summary>
        /// Creates a base64 hash from the current datetime
        /// </summary>
        public static string HashFromCurrentDateTime()
        {
            return HashFromDateTime(DateTime.Now);
        }
        /// <summary>
        /// Returns a hash as a string from the 32-bit hash value of the specified datetime
        /// </summary>
        public static string ShortHashFromDateTime(DateTime date)
        {
            return date.ToString(CultureInfo.InvariantCulture).GetHashCode().ToString("x");
        }
        /// <summary>
        /// Returns a hash as a string from the 32-bit hash value of the current datetime
        /// </summary>
        public static string ShortHashFromCurrentDateTime()
        {
            return DateTime.Now.ToString(CultureInfo.InvariantCulture).GetHashCode().ToString("x");
        }
        /// <summary>
        /// Creates a timestamp from current datetime
        /// </summary>
        public static string CreateTimestamp()
        {
            return DateTime.Now.ToString("yyyyMMddHHmmssFFF");
        }
        /// <summary>
        /// Creates a timestamp from a specified datetime
        /// </summary>
        public static string CreateTimestamp(DateTime date)
        {
            return date.ToString("yyyyMMddHHmmssFFF");
        }
    }
}
