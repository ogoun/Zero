using System;
using System.Collections;
using System.Collections.Generic;

namespace ZeroLevel.HNSW.Services
{
    /// <summary>
    /// Max element always on top
    /// </summary>
    public class MaxHeap :
        IEnumerable<(int, float)>
    {
        private readonly List<(int, float)> _elements;

        public MaxHeap(int size = -1)
        {
            if (size > 0)
                _elements = new List<(int, float)>(size);
            else
                _elements = new List<(int, float)>();
        }

        private int GetLeftChildIndex(int elementIndex) => 2 * elementIndex + 1;
        private int GetRightChildIndex(int elementIndex) => 2 * elementIndex + 2;
        private int GetParentIndex(int elementIndex) => (elementIndex - 1) / 2;

        private bool HasLeftChild(int elementIndex) => GetLeftChildIndex(elementIndex) < _elements.Count;
        private bool HasRightChild(int elementIndex) => GetRightChildIndex(elementIndex) < _elements.Count;
        private bool IsRoot(int elementIndex) => elementIndex == 0;

        private (int, float) GetLeftChild(int elementIndex) => _elements[GetLeftChildIndex(elementIndex)];
        private (int, float) GetRightChild(int elementIndex) => _elements[GetRightChildIndex(elementIndex)];
        private (int, float) GetParent(int elementIndex) => _elements[GetParentIndex(elementIndex)];

        public int Count => _elements.Count;

        public void Clear()
        {
            _elements.Clear();
        }

        private void Swap(int firstIndex, int secondIndex)
        {
            var temp = _elements[firstIndex];
            _elements[firstIndex] = _elements[secondIndex];
            _elements[secondIndex] = temp;
        }

        public bool IsEmpty()
        {
            return _elements.Count == 0;
        }

        public bool TryPeek(out int id, out float value)
        {
            if (_elements.Count == 0)
            {
                id = -1;
                value = 0;
                return false;
            }
            id = _elements[0].Item1;
            value = _elements[0].Item2;
            return true;
        }

        public (int, float) Pop()
        {
            if (_elements.Count == 0)
                throw new IndexOutOfRangeException();

            var result = _elements[0];
            _elements[0] = _elements[_elements.Count - 1];
            _elements.RemoveAt(_elements.Count - 1);

            ReCalculateDown();

            return result;
        }

        public void Push((int, float) element)
        {
            _elements.Add(element);

            ReCalculateUp();
        }

        private void ReCalculateDown()
        {
            int index = 0;
            while (HasLeftChild(index))
            {
                var biggerIndex = GetLeftChildIndex(index);
                if (HasRightChild(index) && GetRightChild(index).Item2 > GetLeftChild(index).Item2)
                {
                    biggerIndex = GetRightChildIndex(index);
                }

                if (_elements[biggerIndex].Item2 < _elements[index].Item2)
                {
                    break;
                }

                Swap(biggerIndex, index);
                index = biggerIndex;
            }
        }

        private void ReCalculateUp()
        {
            var index = _elements.Count - 1;
            while (!IsRoot(index) && _elements[index].Item2 > GetParent(index).Item2)
            {
                var parentIndex = GetParentIndex(index);
                Swap(parentIndex, index);
                index = parentIndex;
            }
        }

        public IEnumerator<(int, float)> GetEnumerator()
        {
            return _elements.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _elements.GetEnumerator();
        }
    }
}
