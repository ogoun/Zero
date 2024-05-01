using System;

namespace ZeroLevel.Services.Collections
{
    public interface IPriorityQueue<T>
    {
        int Count { get; }
        void Append(T item, int priority);
        T HandleCurrentItem();
    }

    public struct PriorityQueueObjectHandleResult
    {
        public bool IsCompleted;
        public bool CanBeSkipped;
    }

    public class ZPriorityQueue<T>
        : IPriorityQueue<T>
    {
        private sealed class PriorityQueueObject<T1>
        {
            public readonly int Priority;

            public readonly T1 Value;

            public PriorityQueueObject<T1> Next;

            public PriorityQueueObject(T1 val, int priority)
            {
                Value = val;
                Priority = priority;
            }
        }

        private readonly Func<T, PriorityQueueObjectHandleResult> _handler;
        private PriorityQueueObject<T> _head = null!;
        private readonly object _rw_lock = new object();
        private int _counter = 0;

        public int Count => _counter;

        public ZPriorityQueue(Func<T, PriorityQueueObjectHandleResult> handler)
        {
            if (handler == null!)
                throw new ArgumentNullException(nameof(handler));
            _handler = handler;
        }

        public void Append(T item, int priority)
        {
            var insert = new PriorityQueueObject<T>(item, priority);
            lock (_rw_lock)
            {
                if (null == _head)
                {
                    _head = insert;
                }
                else
                {
                    var cursor = _head;
                    PriorityQueueObject<T> prev = null!;
                    do
                    {
                        if (cursor.Priority > insert.Priority)
                        {
                            insert.Next = cursor;
                            if (null == prev) // insert to head
                            {
                                _head = insert;
                            }
                            else
                            {
                                prev.Next = insert;
                            }
                            break;
                        }
                        prev = cursor;
                        cursor = cursor.Next;
                        if (cursor == null!)
                        {
                            prev.Next = insert;
                        }
                    } while (cursor != null!);
                }
                _counter++;
            }
            return;
        }

        public T HandleCurrentItem()
        {
            T v = default(T)!;
            lock (_rw_lock)
            {
                var item = _head;
                PriorityQueueObject<T> prev = null!;
                while (item != null!)
                {
                    var result = this._handler.Invoke(item.Value);
                    if (result.IsCompleted)
                    {
                        if (prev != null!)
                        {
                            prev.Next = item.Next;
                        }
                        else
                        {
                            _head = _head.Next;
                        }
                        v = item.Value;
                        break;
                    }

                    if (result.CanBeSkipped == false)
                    {
                        break;
                    }

                    prev = item;
                    item = item.Next;
                }
            }
            return v;
        }
    }
}
