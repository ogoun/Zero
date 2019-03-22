using System;
using System.Collections.Generic;
using System.Linq;

namespace ZeroLevel.Services.Collections
{
    /// <summary>
    /// Циклический разреженный итератор
    /// позволяет выполнять циклический обход массива, с возможностью отмечать элементы
    /// которые требуется прпускать при следующих обходах.
    /// </summary>
    public class SparseIterator<T>
    {
        private readonly T[] _array;
        private readonly HashSet<int> _removed = new HashSet<int>();
        private int index = -1;

        public SparseIterator(IEnumerable<T> items)
        {
            _array = items.ToArray();
        }
        /// <summary>
        /// Текущий элемент последовательности
        /// </summary>
        public T Current
        {
            get
            {
                if (index >= 0 && index < _array.Length)
                {
                    return _array[index];
                }
                throw new IndexOutOfRangeException();
            }
        }
        /// <summary>
        /// Указывает на отсутствие элементов в последовательности или на
        /// то что все элементы были отмечены для пропуска
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return _array.Length == 0 || _array.Length == _removed.Count;
            }
        }
        /// <summary>
        /// Сдвиг на следующий элемент, если достигнут конец последовательности,
        /// переключается на первый неотмеченный для пропуска элемент
        /// </summary>
        /// <returns>вернет -1 если последовательность пуста, или если не осталось элементов не отмеченных для пропуска</returns>
        public int MoveNext()
        {
            do
            {
                index++;
            } while (_removed.Contains(index));
            if (index >= _array.Length)
            {
                if (IsEmpty) return -1;
                index = -1;
                do
                {
                    index++;
                } while (_removed.Contains(index));
            }
            return index;
        }
        /// <summary>
        /// Отмечает текущий элемент для пропуска при следующем обходе
        /// </summary>
        /// <returns></returns>
        public bool Exclude()
        {
            if (index >= 0)
            {
                return _removed.Add(index);
            }
            return false;
        }
    }
}
