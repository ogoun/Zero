using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using ZeroLevel.Services.Collections;
using System.Collections.Concurrent;

namespace ZeroLevel.Services.Utils
{
    public class MultiHandler<T>
        : IDisposable
    {
        private class Executor
        {
            public Thread Thread;
            public Action<T> Handle;
            public Func<int> Count;
            public bool Canceled;
        }

        private RoundRobinCollection<Executor> _handlers = new RoundRobinCollection<Executor>();
        private readonly object _lock_handlers = new object();

        public MultiHandler(Action<T> handler, int size)
        {
            for (int i = 0; i < size; i++)
            {
                var t = new Thread((s) =>
                {
                    var queue = new BlockingCollection<T>();
                    var executor = new Executor
                    {
                        Handle = data =>
                        {
                            queue.Add(data);
                        },
                        Count = () => queue.Count,
                        Thread = Thread.CurrentThread,
                        Canceled = false
                    };
                    lock (_lock_handlers)
                    {
                        ((RoundRobinCollection<Executor>)s).Add(executor);
                    }
                    try
                    {
                        while (!executor.Canceled)
                        {
                            T obj;
                            if (queue.TryTake(out obj, 500))
                            {
                                handler(obj);
                            }
                        }
                        queue.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "[MultiHandler] Loop fault");
                    }
                });
                t.IsBackground = true;
                t.Start(_handlers);
            }
        }

        public void Append(T t)
        {
            _handlers.MoveNextAndHandle(h => h.Handle(t));
        }

        private int Count => _handlers.Source.Sum(h => h.Count());

        public void WaitForEmpty()
        {
            while (Count > 0)
            {
                Thread.Sleep(300);
            }
        }

        public void Dispose()
        {
            foreach (var h in _handlers.Source) h.Canceled = true;
        }
    }
}
