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
        /// Action at the end of the wait
        /// </summary>
        public Func<long, Task> Callback;

        /// <summary>
        /// Expiration Timeout
        /// </summary>
        public DateTime ExpirationDate;

        /// <summary>
        /// Next object with the nearest waiting date
        /// </summary>
        public ExpiredAsyncObject Next;

        /// <summary>
        /// Key to identify the pending event
        /// </summary>
        public long Key { get; }
    }
}