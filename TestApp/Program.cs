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
        private class LogQueueWrapper
        {
            private string TakeMethod;
            private string PushMethod;
            private object Target;
            public IInvokeWrapper Invoker;

            public LogMessage<T> Take<T>()
            {
                return (LogMessage<T>)Invoker.Invoke(Target, TakeMethod);
            }

            public void Push<T>(LogLevel level, LogMessage<T> value)
            {
                Invoker.Invoke(Target, PushMethod, new object[] { level, value });
            }

            public static LogQueueWrapper Create<T>(object target)
            {
                var wrapper = new LogQueueWrapper { Invoker = InvokeWrapper.Create(), Target = target };
                wrapper.PushMethod = wrapper.Invoker.ConfigureGeneric<NoLimitedLogMessageBuffer>(typeof(T), "Push").First();
                wrapper.TakeMethod = wrapper.Invoker.ConfigureGeneric<NoLimitedLogMessageBuffer>(typeof(T), "Take").First();
                return wrapper;
            }
        }

        private static Func<MethodInfo, bool> CreateArrayPredicate<T>()
        {
            var typeArg = typeof(T).GetElementType();
            return mi => mi.Name.Equals("WriteArray", StringComparison.Ordinal) &&
                mi.GetParameters().First().ParameterType.GetElementType().IsAssignableFrom(typeArg);
        }

        private static Func<MethodInfo, bool> CreateCollectionPredicate<T>()
        {
            var typeArg = typeof(T).GetGenericArguments().First();
            return mi => mi.Name.Equals("WriteCollection", StringComparison.Ordinal) &&
            mi.GetParameters().First().ParameterType.GetGenericArguments().First().IsAssignableFrom(typeArg);
        }

        private static void Main(string[] args)
        {
            var wrapper = new Wrapper { Invoker = InvokeWrapper.Create() };
            var ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadDateTimeArray").First();
            var WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateArrayPredicate<DateTime?[]>()).First();

            Console.Write(WriteId);
        }
    }
}