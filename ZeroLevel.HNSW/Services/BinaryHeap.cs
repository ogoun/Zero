using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ZeroLevel.HNSW
{
    /// <summary>
    /// Binary heap wrapper around the <see cref="IList{T}"/>
    /// It's a max-heap implementation i.e. the maximum element is always on top.
    /// </summary>
    /// <typeparam name="T">The type of the items in the source list.</typeparam>
    public class BinaryHeap :
        IEnumerable<(int, float)>
    {
        private static BinaryHeap _empty = new BinaryHeap();

        public static BinaryHeap Empty => _empty;

        private readonly List<(int, float)> _data;

        private bool _frozen = false;
        public (int, float) Nearest => _data[_data.Count - 1];
        public (int, float) Farthest => _data[0];

        public (int, float) PopNearest()
        {
            if (this._data.Any())
            {
                var result = this._data[this._data.Count - 1];
                this._data.RemoveAt(this._data.Count - 1);
                return result;
            }
            return (-1, -1);
        }

        public (int, float) PopFarthest()
        {
            if (this._data.Any())
            {
                var result = this._data.First();
                this._data[0] = this._data.Last();
                this._data.RemoveAt(this._data.Count - 1);
                this.SiftDown(0);
                return result;
            }
            return (-1, -1);
        }

        public int Count => _data.Count;
        public void Clear() => _data.Clear();

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryHeap{T}"/> class.
        /// </summary>
        /// <param name="buffer">The buffer to store heap items.</param>
        public BinaryHeap(int k = -1, bool frozen = false)
        {
            _frozen = frozen;
            if (k > 0)
                _data = new List<(int, float)>(k);
            else
                _data = new List<(int, float)>();
        }

        /// <summary>
        /// Pushes item to the heap.
        /// </summary>
        /// <param name="item">The item to push.</param>
        public void Push(int item, float distance)
        {
            this._data.Add((item, distance));
            this.SiftUp(this._data.Count - 1);
        }

        /// <summary>
        /// Pops the item from the heap.
        /// </summary>
        /// <returns>The popped item.</returns>
        public (int, float) Pop()
        {
            if (this._data.Any())
            {
                var result = this._data.First();

                this._data[0] = this._data.Last();
                this._data.RemoveAt(this._data.Count - 1);
                this.SiftDown(0);

                return result;
            }

            throw new InvalidOperationException("Heap is empty");
        }

        /// <summary>
        /// Restores the heap property starting from i'th position down to the bottom
        /// given that the downstream items fulfill the rule.
        /// </summary>
        /// <param name="i">The position of item where heap property is violated.</param>
        private void SiftDown(int i)
        {
            while (i < this._data.Count)
            {
                int l = (2 * i) + 1;
                int r = l + 1;
                if (l >= this._data.Count)
                {
                    break;
                }
                int m = ((r < this._data.Count) && this._data[l].Item2 < this._data[r].Item2) ? r : l;
                if (this._data[m].Item2 <= this._data[i].Item2)
                {
                    break;
                }
                this.Swap(i, m);
                i = m;
            }
        }

        /// <summary>
        /// Restores the heap property starting from i'th position up to the head
        /// given that the upstream items fulfill the rule.
        /// </summary>
        /// <param name="i">The position of item where heap property is violated.</param>
        private void SiftUp(int i)
        {
            while (i > 0)
            {
                int p = (i - 1) / 2;
                if (this._data[i].Item2 <= this._data[p].Item2)
                {
                    break;
                }
                this.Swap(i, p);
                i = p;
            }
        }

        /// <summary>
        /// Swaps items with the specified indicies.
        /// </summary>
        /// <param name="i">The first index.</param>
        /// <param name="j">The second index.</param>
        private void Swap(int i, int j)
        {
            var temp = this._data[i];
            this._data[i] = this._data[j];
            this._data[j] = temp;
        }

        public IEnumerator<(int, float)> GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _data.GetEnumerator();
        }
    }
}
