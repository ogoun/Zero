using System;
using System.Threading;

namespace ZeroLevel.Services.Shedulling
{
    /// <summary>
    /// Обертка над Action, хранящее время в которое должно быть выполнено действие,
    /// а также ссылку на следующее действие
    /// </summary>
    internal class ExpiredObject
    {
        private static long _counter = 0;

        public ExpiredObject()
        {
            Key = Interlocked.Increment(ref _counter);
            if(Key == -1)
                Key = Interlocked.Increment(ref _counter);
        }

        public ExpiredObject(bool has_no_key)
        {
            if (has_no_key)
                Key = -1;
            else
                Key = Interlocked.Increment(ref _counter);
        }

        public ExpiredObject Reset(DateTime nextDate)
        {
            ExpirationDate = nextDate;
            Next = null;
            return this;
        }

        /// <summary>
        /// Событие при завершении ожидания
        /// </summary>
        public Action<long> Callback;
        /// <summary>
        /// Срок истечения ожидания
        /// </summary>
        public DateTime ExpirationDate;
        /// <summary>
        /// Следующий объект с ближайшей датой окончания ожидания
        /// </summary>
        public ExpiredObject Next;
        /// <summary>
        /// Ключ для идентификации ожидающего события
        /// </summary>
        public long Key { get; }
    }
}
