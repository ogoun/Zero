using System;
using System.Threading;

namespace ZeroLevel.Services.Shedulling
{
    /// <summary>
    /// A wrapper around an Action that stores the time at which an action should be performed, as well as a link to the next action.
    /// </summary>
    internal sealed class ExpiredObject
    {
        private static long _counter = 0;

        internal static void ResetIndex(long index)
            => _counter = index;

        public ExpiredObject()
        {
            Key = Interlocked.Increment(ref _counter);
            if (Key == -1)
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
        /// Action at the end of the wait
        /// </summary>
        public Action<long> Callback;

        /// <summary>
        ///Expiration Timeout
        /// </summary>
        public DateTime ExpirationDate;

        /// <summary>
        /// Next object with the nearest waiting date
        /// </summary>
        public ExpiredObject Next;

        /// <summary>
        /// Key to identify the pending event
        /// </summary>
        public long Key { get; }
    }
}