using System.Collections.Generic;
using System.Linq;

namespace ZeroLevel.Services.Trees
{
    public class GNode<T>
    {
        public string Id { get; set; }
        public T Value { get; set; }
        public readonly Dictionary<string, GNode<T>> Nodes = new Dictionary<string, GNode<T>>();

        public void Add(string[] path, int index, T value)
        {
            if (path.Length > index)
            {
                if (false == Nodes.ContainsKey(path[index]))
                {
                    Nodes[path[index]] = new GNode<T>();
                }
                if (path.Length == (index + 1))
                {
                    Nodes[path[index]].Value = value;
                }
                else
                {
                    Nodes[path[index]].Add(path, index + 1, value);
                }
            }
        }
    }

    public class GTree<T>
    {
        private readonly Dictionary<string, GNode<T>> _rootNodes = new Dictionary<string, GNode<T>>();
        public IEnumerable<GNode<T>> RootNodes => _rootNodes.Values.ToArray();

        public void Add(string[] path, T value)
        {
            if (path.Length > 0)
            {
                if (false == _rootNodes.ContainsKey(path[0]))
                {
                    _rootNodes[path[0]] = new GNode<T>();
                }
                if (path.Length == 1)
                {
                    _rootNodes[path[0]].Value = value;
                }
                else
                {
                    _rootNodes[path[0]].Add(path, 1, value);
                }
            }
        }

        /// <summary>
        /// traversing a tree in width from left to right
        /// </summary>
        public IEnumerable<GNode<T>> Plain()
        {
            var queue = new Queue<GNode<T>>();
            foreach (var r in _rootNodes) {
                queue.Enqueue(r.Value);
            }
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                yield return current;
                foreach (var r in current.Nodes)
                {
                    queue.Enqueue(r.Value);
                }
            }
        }
    }
}