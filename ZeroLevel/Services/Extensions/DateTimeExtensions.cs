using System;

namespace ZeroLevel
{
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Огругляет дату до указанного масштаба
        /// 
        /// Необходимость: округление дат при сохранении в модель, которая будет использоваться для SqlServer,
        /// т.к. sqlserver не может точность - Datetime values are rounded to increments of .000, .003, or .007 seconds
        /// </summary>
        public static DateTime Truncate(this DateTime dateTime, TimeSpan timeSpan)
        {
            if (timeSpan == TimeSpan.Zero) return dateTime; // Or could throw an ArgumentException
            return dateTime.AddTicks(-(dateTime.Ticks % timeSpan.Ticks));
        }
    }
}
