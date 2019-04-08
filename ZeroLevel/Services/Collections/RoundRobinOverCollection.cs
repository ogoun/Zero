using System;
using System.Collections.Generic;
using System.Linq;

namespace ZeroLevel.Services.Collections
{
    public sealed class RoundRobinOverCollection<T>
    {
        private class Node
        {
            public T Value;
            public Node Next;
        }

        private int _count;
        private Node _currentNode;

        public bool IsEmpty => _count <= 0;

        public RoundRobinOverCollection(IEnumerable<T> collection)
        {
            if (collection.Any())
            {
                _count = 1;
                _currentNode = new Node { Value = collection.First() };
                var prev = _currentNode;
                foreach (var e in collection.Skip(1))
                {
                    prev.Next = new Node { Value = e };
                    prev = prev.Next;
                    _count++;
                }
                prev.Next = _currentNode;
            }
            else
            {
                _count = 0;
            }
        }

        public IEnumerable<T> Find(Func<T, bool> selector)
        {
            if (_count == 0)
            {
                yield break;
            }
            var cursor = _currentNode;
            for (int i = 0; i < _count; i++)
            {
                if (selector(cursor.Value))
                {
                    yield return cursor.Value;
                }
                cursor = cursor.Next;
            }
        }

        public IEnumerable<T> GenerateSeq()
        {
            if (_count == 0)
            {
                yield break;
            }
            var cursor = _currentNode;
            _currentNode = _currentNode.Next;
            for (int i = 0; i < _count; i++)
            {
                yield return cursor.Value;
                cursor = cursor.Next;
            }
        }
    }
}
