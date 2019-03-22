using System;
using System.Collections.Generic;
using System.Linq;
using ZeroLevel.Services.Collections;

namespace ZeroLevel.Services.Trees
{
    /// <summary>
    /// Выполняет преобразование набора элементов в дерево (в набор ветвей)
    /// </summary>
    /// <typeparam name="T">Тип элемента</typeparam>
    /// <typeparam name="TKey">Тип связующих компонентов элемента</typeparam>
    public class ItemFieldToTreeConverter<T, TKey>
    {
        #region Inner classes
        private struct BranchTestResult
        {
            public bool HasInteraction;
            public NodeBranch NewBranch;
        }

        private struct Node
        {
            public T Value;
            public TKey In;
            public TKey Out;

            internal Node Clone()
            {
                return new Node
                {
                    In = this.In,
                    Out = this.Out,
                    Value = this.Value
                };
            }

            public bool Eq(Node other, Func<T, T, bool> comparer, Func<TKey, TKey, bool> key_comparer)
            {
                if (ReferenceEquals(this, other))
                    return true;
                return comparer(this.Value, other.Value) &&
                    key_comparer(this.In, other.In) &&
                    key_comparer(this.Out, other.Out);
            }

            public override int GetHashCode()
            {
                return this.Value?.GetHashCode() ?? 0 ^ this.In.GetHashCode() ^ this.Out.GetHashCode();
            }
        }

        private class NodeBranch
        {
            private List<Node> _nodes = new List<Node>();
            private readonly Func<TKey, TKey, bool> _key_comparer;
            private readonly Func<T, T, bool> _comparer;
            public IEnumerable<Node> Nodes
            {
                get
                {
                    return _nodes;
                }
            }

            public NodeBranch(Node first, Func<T, T, bool> comparer, Func<TKey, TKey, bool> key_comparer)
            {
                _nodes.Add(first);
                _key_comparer = key_comparer;
                _comparer = comparer;
            }

            public NodeBranch(IEnumerable<Node> nodes, Func<T, T, bool> comparer, Func<TKey, TKey, bool> key_comparer)
            {
                _nodes.AddRange(nodes);
                _key_comparer = key_comparer;
                _comparer = comparer;
            }

            public List<T> Extract()
            {
                return _nodes.Select(n => n.Value).ToList();
            }

            public BranchTestResult Test(Node node)
            {
                var result = new BranchTestResult { HasInteraction = false, NewBranch = null };
                if (_key_comparer(_nodes.Last().Out, node.In))
                {
                    _nodes.Add(node);
                    result.HasInteraction = true;
                }
                else if (_key_comparer(_nodes.First().In, node.Out))
                {
                    _nodes.Insert(0, node);
                    result.HasInteraction = true;
                }
                else
                {
                    for (int i = 0; i < _nodes.Count; i++)
                    {
                        var current = _nodes[i];
                        if (_key_comparer(current.Out, node.In))
                        {
                            var list = _nodes.Take(i + 1).ToList();
                            list.Add(node);
                            result.NewBranch = new NodeBranch(list, _comparer, _key_comparer);
                            result.HasInteraction = true;
                            break;
                        }
                        else if (_key_comparer(current.In, node.Out))
                        {
                            var list = _nodes.Skip(i).ToList();
                            list.Insert(0, node);
                            result.NewBranch = new NodeBranch(list, _comparer, _key_comparer);
                            result.HasInteraction = true;
                            break;
                        }
                    }
                }
                return result;
            }
            /// <summary>
            /// Проверка возможности объединения с другой ветвью, если текущая является ее началом или продолжением
            /// </summary>
            public bool TryJoin(NodeBranch other)
            {
                if (other == null)
                {
                    return false;
                }
                if (this._nodes.First().Eq(other._nodes.Last(), _comparer ,_key_comparer))
                {
                    this._nodes = other._nodes.Union(this._nodes).ToList();
                    return true;
                }
                else if (this._nodes.Last().Eq(other._nodes.First(), _comparer, _key_comparer))
                {
                    this._nodes = this._nodes.Union(other._nodes).ToList();
                    return true;
                }
                return false;
            }

            public override bool Equals(object obj)
            {
                return this.Equals(obj as NodeBranch);
            }

            public override int GetHashCode()
            {
                return this._nodes.GetEnumHashCode();
            }

            public bool Equals(NodeBranch other)
            {
                if (other == null)
                    return false;
                if (ReferenceEquals(this, other))
                    return true;

                return this._nodes.OrderingEquals(other._nodes);
            }
        }
        #endregion

        private readonly Func<T, TKey> _inKeyExtractor;
        private readonly Func<T, TKey> _outKeyExtractor;
        private readonly Func<T, T, bool> _comparer;
        private readonly Func<TKey, TKey, bool> _key_comparer;
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="inKeyExtractor">Экстрактор входной связи элемента</param>
        /// <param name="outKeyExtractor">Экстрактор исходящей связи элемента</param>
        public ItemFieldToTreeConverter(Func<T, TKey> inKeyExtractor,
            Func<T, TKey> outKeyExtractor,
            Func<T, T, bool> comparer = null,
            Func<TKey, TKey, bool> key_comparer = null)
        {
            if (inKeyExtractor == null)
            {
                throw new ArgumentNullException(nameof(inKeyExtractor));
            }
            if (outKeyExtractor == null)
            {
                throw new ArgumentNullException(nameof(outKeyExtractor));
            }
            _inKeyExtractor = inKeyExtractor;
            _outKeyExtractor = outKeyExtractor;
            _comparer = comparer;
            _key_comparer = key_comparer ?? new Func<TKey, TKey, bool>((k1, k2) =>
            {
                if (k1 == null && k2 == null) return true;
                if (k1 == null) return false;
                if (k2 == null) return false;
                return k1.Equals(k2);
            });
        }
        /// <summary>
        /// Преобразование набора элементов к набору ветвей
        /// </summary>
        public IEnumerable<List<T>> Convert(IEnumerable<T> entries)
        {
            if (entries == null || entries.Any() == false)
            {
                return Enumerable.Empty<List<T>>();
            }
            var iterator = new SparseIterator<T>(entries);
            var result = new List<NodeBranch>();
            if (iterator.MoveNext() != -1)
            {
                result.Add(new NodeBranch(new Node
                {
                    Value = iterator.Current,
                    In = _inKeyExtractor(iterator.Current),
                    Out = _outKeyExtractor(iterator.Current)
                }, _comparer, _key_comparer));
                iterator.Exclude();
            }
            else
            {
                return Enumerable.Empty<List<T>>();
            }
            int index;
            var cachee = new Dictionary<int, Node>();
            while ((index = iterator.MoveNext()) != -1)
            {
                if (cachee.ContainsKey(index) == false)
                {
                    cachee.Add(index, new Node
                    {
                        Value = iterator.Current,
                        In = _inKeyExtractor(iterator.Current),
                        Out = _outKeyExtractor(iterator.Current)
                    });
                }
                var node = cachee[index];
                bool included = false;
                var include = new List<NodeBranch>();
                foreach (var branch in result)
                {
                    var tr = branch.Test(node);
                    if (tr.HasInteraction)
                    {
                        included = true;
                        if (tr.NewBranch != null)
                        {
                            include.Add(tr.NewBranch);
                        }
                    }
                }
                if (included == false)
                {
                    result.Add(new NodeBranch(node, _comparer, _key_comparer));
                }
                iterator.Exclude();
                if (include.Count > 0) result.AddRange(include);
            }
            // Проверить, если одна ветка является началом, или продолжением другой, выполнить склейки
            for (int i = 0; i < result.Count - 1; i++)
            {
                var left = result[i];
                for (int j = i + 1; j < result.Count; j++)
                {
                    var right = result[j];
                    if (IsNodeBrunchEquals(left, right) || left.TryJoin(right))
                    {
                        result.RemoveAt(j); j--;
                    }
                }
            }
            return result.Select(e => e.Extract());
        }

        private bool IsNodeBrunchEquals(NodeBranch first, NodeBranch second)
        {
            if (first == null && second == null)
                return true;
            if (first == null)
                return false;
            if (second == null)
                return false;
            if (_comparer == null)
                return first.Equals(second);
            if (ReferenceEquals(first, second))
                return true;

            var f_arr = first.Nodes.ToArray();
            var s_arr = second.Nodes.ToArray();
            if (f_arr.Length != s_arr.Length)
                return false;
            for (int i = 0; i < f_arr.Length; i++)
            {
                var fi = f_arr[i];
                var si = s_arr[i];
                if (_key_comparer(fi.In, si.In) == false) return false;
                if (_key_comparer(fi.Out, si.Out) == false) return false;
                if (_comparer(fi.Value, si.Value) == false) return false;
            }
            return true;
        }
    }
}
