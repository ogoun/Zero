using System;
using System.Threading;
using ZeroLevel.Services.Collections;

namespace ZeroLevel.Services.Logging
{
    internal sealed class FixSizeLogMessageBuffer
        : ILogMessageBuffer
    {
        private readonly FixSizeQueue<Tuple<LogLevel, string>> _queue;
        private readonly ManualResetEvent _waitItemsFlag = new ManualResetEvent(false);

        public FixSizeLogMessageBuffer(long backlog)
        {
            _queue = new FixSizeQueue<Tuple<LogLevel, string>>(backlog);
        }

        public long Count
        {
            get
            {
                return _queue.Count;
            }
        }

        public void Dispose()
        {
            _waitItemsFlag.Dispose();
        }

        public void Push(LogLevel level, string message)
        {
            _queue.Push(new Tuple<LogLevel, string>(level, message));
            _waitItemsFlag.Set();
        }

        public Tuple<LogLevel, string> Take()
        {
            while (_queue.Count == 0)
                _waitItemsFlag.WaitOne(110);
            var t = _queue.Take();
            if (_queue.Count == 0) _waitItemsFlag.Reset();
            return t;
        }
    }
}