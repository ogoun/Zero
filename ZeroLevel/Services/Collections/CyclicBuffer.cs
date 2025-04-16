using System;

namespace ZeroLevel.Services.Collections
{
    /// <summary>
    /// Циклический буфер
    /// </summary>
    public class CyclicBuffer<T>
    {
        private T[] _buffer;
        private int _head;
        private int _tail;

        public CyclicBuffer(int size)
        {
            if (size <= 0)
                throw new ArgumentException($"{nameof(size)} must be positive.");
            _buffer = new T[size];
            _head = 0;
            _tail = 0;
        }

        public bool IsFull { get { return (_tail + 1) % _buffer.Length == _head; } }
        public bool IsEmpty { get { return _head == _tail; } }

        public void Enqueue(T item)
        {
            _buffer[_tail] = item;
            _tail = (_tail + 1) % _buffer.Length;

            // Если буфер полон, сдвигаем head 
            if (IsFull)
                _head = (_head + 1) % _buffer.Length;
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                int effectiveIndex = (_head + index) % _buffer.Length;
                return _buffer[effectiveIndex];
            }
        }

        public int Count
        {
            get { return (_tail >= _head) ? _tail - _head : _buffer.Length + _tail - _head; }
        }
    }
}
