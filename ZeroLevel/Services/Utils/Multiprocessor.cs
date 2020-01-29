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
        private bool _is_disposed = false;
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
                                    handler(item);
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

        public void Append(T t) => _queue.Add(t);

        public void WaitForEmpty()
        {
            while (_queue.Count > 0)
            {
                Thread.Sleep(100);
            }
        }

        public void Dispose()
        {
            _queue.CompleteAdding();
            while (_queue.Count > 0 || _tasks_in_progress > 0)
            {
                Thread.Sleep(100);
            }
            _is_disposed = true;
            Thread.Sleep(1000); // wait while threads exit
            foreach (var thread in _threads) thread.Join();
            _queue.Dispose();
        }
    }
}
