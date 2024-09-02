using System;
using System.Collections.Concurrent;
using ZeroLevel.Logging;
using ZeroLevel.Services.HashFunctions;
using ZeroLevel.Services.Mathemathics;

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
            var date = DateTime.Now;

            var bytes = new byte[1531];
            new Random().NextBytes(bytes);
            var hash = Murmur3.ComputeULongHash(bytes);
            Console.WriteLine($"{hash}");

            new Random().NextBytes(bytes);
            hash = Murmur3.ComputeULongHash(bytes);
            Console.WriteLine($"{hash}");

            bytes[0] = 10;
            hash = Murmur3.ComputeULongHash(bytes);
            Console.WriteLine($"{hash}");

            new Random().NextBytes(bytes);
            hash = Murmur3.ComputeULongHash(bytes);
            Console.WriteLine($"{hash}");
            Console.ReadKey();

            /*
            foreach (var c in Combinations.GenerateUniqueSets(new int[] { 1, 2, 3, 4, 5, 6 }, 3))
            { 
                Console.WriteLine(string.Join('\t', c));
            }
            */
        }
    }
}