using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace ZeroLevel.Utils
{
    public class Multiprocessor<T>
         : IDisposable
    {
        private BlockingCollection<T> _queue = new BlockingCollection<T>();
        private List<Thread> _threads = new List<Thread>();
        private volatile bool _is_disposed = false;
        private int _tasks_in_progress = 0;
        public int Count => _queue.Count + _tasks_in_progress;

        public Multiprocessor(Action<T> handler, int size, int stackSize = 1024 * 1024)
        {
            for (int i = 0; i < size; i++)
            {
                var t = new Thread(() =>
                {
                    T item;
                    while (!_is_disposed)
                    {
                        try
                        {
                            if (_queue.TryTake(out item, 500))
                            {
                                Interlocked.Increment(ref _tasks_in_progress);
                                try
                                {
                                    handler?.Invoke(item);
                                }
                                finally
                                {
                                    Interlocked.Decrement(ref _tasks_in_progress);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "[Multiprocessor.HandleThread]");
                        }
                    }
                }, stackSize);
                t.IsBackground = true;
                _threads.Add(t);
            }
            foreach (var t in _threads) t.Start();
        }

        public void Append(T t) { if (!_is_disposed) _queue.Add(t); }

        public bool WaitForEmpty(int timeoutInMs)
        {
            var start = DateTime.UtcNow;
            while (Count > 0)
            {
                if (timeoutInMs > 0)
                {
                    if ((DateTime.UtcNow - start).TotalMilliseconds > timeoutInMs)
                    {
                        return false;
                    }
                }
                Thread.Sleep(100);
            }
            return true;
        }

        public void WaitForEmpty()
        {
            while (Count > 0)
            {
                Thread.Sleep(200);
            }
        }

        public void Dispose()
        {
            _is_disposed = true;
            _queue.Dispose();
            _threads = null!;
        }
    }
}
