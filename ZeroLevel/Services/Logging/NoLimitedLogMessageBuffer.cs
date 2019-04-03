using System;
using System.Collections.Concurrent;

namespace ZeroLevel.Services.Logging
{
    internal sealed class NoLimitedLogMessageBuffer : ILogMessageBuffer
    {
        private readonly BlockingCollection<Tuple<LogLevel, string>> _messageQueue =
            new BlockingCollection<Tuple<LogLevel, string>>();

        private bool _isDisposed = false;

        public long Count
        {
            get
            {
                if (_messageQueue.IsCompleted)
                    return 0;
                return _messageQueue.Count;
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _messageQueue.Dispose();
            }
        }

        public void Push(LogLevel level, string message)
        {
            if (_isDisposed) return;
            _messageQueue.Add(new Tuple<LogLevel, string>(level, message));
        }

        public Tuple<LogLevel, string> Take()
        {
            return _messageQueue.Take();
        }
    }
}