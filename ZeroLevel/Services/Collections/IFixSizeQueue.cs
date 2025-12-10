using System.Collections.Generic;

namespace ZeroLevel.Services.Collections
{
    public interface IFixSizeQueue<T>
    {
        void Push(T item);
        long Count { get; }
        bool TryTake(out T t);
        T Take();
        IEnumerable<T> Dump();
        T Peek();
        bool TryPeek(out T item);
        T GetLast();
        bool TryGetLast(out T item);
        bool Contains(T item, IComparer<T> comparer = null!);
    }
}