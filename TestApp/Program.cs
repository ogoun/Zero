using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using ZeroLevel.Logging;
using ZeroLevel.Services.Invokation;
using ZeroLevel.Services.Serialization;

namespace TestApp
{
    public record LogMessage<T>(LogLevel Level, T Message);
    internal interface ILogMessageBuffer<T>
        : IDisposable
    {
        long Count();
        void Push(LogLevel level, T message);
        LogMessage<T> Take();
    }
    internal sealed class NoLimitedLogMessageBuffer<T>
        : ILogMessageBuffer<T>
    {
        private readonly BlockingCollection<LogMessage<T>> _messageQueue =
            new BlockingCollection<LogMessage<T>>();

        private bool _isDisposed = false;

        public long Count()
        {
            if (_messageQueue.IsCompleted)
                return 0;
            return _messageQueue.Count;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _messageQueue.Dispose();
            }
        }

        public void Push(LogLevel level, T message)
        {
            if (_isDisposed) return;
            _messageQueue.Add(new LogMessage<T>(level, message));
        }

        public LogMessage<T> Take()
        {
            return _messageQueue.Take();
        }
    }

    internal static class Program
    {
       

        private static void Main(string[] args)
        {
        }
    }
}