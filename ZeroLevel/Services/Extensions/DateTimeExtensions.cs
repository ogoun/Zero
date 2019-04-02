using System;

namespace ZeroLevel
{
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Rounds the date to the specified scale.
        /// Example: sqlserver - Datetime values are rounded to increments of .000, .003, or .007 seconds
        /// </summary>
        public static DateTime Truncate(this DateTime dateTime, TimeSpan timeSpan)
        {
            if (timeSpan == TimeSpan.Zero) return dateTime; // Or could throw an ArgumentException
            return dateTime.AddTicks(-(dateTime.Ticks % timeSpan.Ticks));
        }
    }
}
