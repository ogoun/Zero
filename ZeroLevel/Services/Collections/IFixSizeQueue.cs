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

        bool Contains(T item, IComparer<T> comparer = null!);
    }
}