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

        public Multiprocessor(Action<T> handler, int size, int stackSize = 1024 * 256)
        {
            for (int i = 0; i < size; i++)
            {
                var t = new Thread(() =>
                {
                    try
                    {
                        T item;
                        while (!_queue.IsCompleted)
                        {
                            if (_queue.TryTake(out item, 200))
                            {
                                handler(item);
                            }
                        }
                    }
                    catch { }
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
            while (_queue.Count > 0)
            {
                Thread.Sleep(100);
            }
            Thread.Sleep(3000); // wait while threads exit
            _queue.Dispose();
        }
    }
}
