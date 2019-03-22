using System;
using System.Globalization;
using System.Threading;

namespace ZeroLevel.Services
{
    /// <summary>
    /// Предоставляет различные варианты генерации идентификаторов
    /// </summary>
    public static class IdGenerator
    {
        /// <summary>
        /// Возвращает функцию для получения последовательных значений int64
        /// </summary>
        public static Func<long> IncreasingSequenceIdGenerator()
        {
            long id = 0;
            return new Func<long>(() => Interlocked.Increment(ref id));
        }
        /// <summary>
        /// Создает Base64 хэш от указанного даты/времени
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
        /// Создает Base64 хэш от текущего даты/времени
        /// </summary>
        public static string HashFromCurrentDateTime()
        {
            return HashFromDateTime(DateTime.Now);
        }
        /// <summary>
        /// Возвращает хэш в виде строки от 32-хбитного значения хэша указанного даты/времени
        /// </summary>
        public static string ShortHashFromDateTime(DateTime date)
        {
            return date.ToString(CultureInfo.InvariantCulture).GetHashCode().ToString("x");
        }
        /// <summary>
        /// Возвращает хэш в виде строки от 32-хбитного значения хэша текущего даты/времени
        /// </summary>
        public static string ShortHashFromCurrentDateTime()
        {
            return DateTime.Now.ToString(CultureInfo.InvariantCulture).GetHashCode().ToString("x");
        }
        /// <summary>
        /// Создает временную отметку из текущей даты/времени
        /// </summary>
        public static string CreateTimestamp()
        {
            return DateTime.Now.ToString("yyyyMMddHHmmssFFF");
        }
        /// <summary>
        /// Создает временную отметку из указанной даты/времени
        /// </summary>
        public static string CreateTimestamp(DateTime date)
        {
            return date.ToString("yyyyMMddHHmmssFFF");
        }
    }
}
