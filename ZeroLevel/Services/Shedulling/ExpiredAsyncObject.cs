using System;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroLevel.Services.Shedulling
{
    internal class ExpiredAsyncObject
    {
        private static long _counter = 0;

        public ExpiredAsyncObject()
        {
            Key = Interlocked.Increment(ref _counter);
        }

        public ExpiredAsyncObject(bool has_no_key)
        {
            if (has_no_key)
                Key = -1;
            else
                Key = Interlocked.Increment(ref _counter);
        }

        public ExpiredAsyncObject Reset(DateTime nextDate)
        {
            ExpirationDate = nextDate;
            Next = null;
            return this;
        }

        /// <summary>
        /// Событие при завершении ожидания
        /// </summary>
        public Func<long, Task> Callback;
        /// <summary>
        /// Срок истечения ожидания
        /// </summary>
        public DateTime ExpirationDate;
        /// <summary>
        /// Следующий объект с ближайшей датой окончания ожидания
        /// </summary>
        public ExpiredAsyncObject Next;
        /// <summary>
        /// Ключ для идентификации ожидающего события
        /// </summary>
        public long Key { get; }
    }
}
