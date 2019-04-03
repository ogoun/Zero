using System;
using System.Collections.Generic;
using System.Linq;

namespace ZeroLevel.Services.Trees
{
    public class Tree<T>
    {
        private readonly Dictionary<string, TreeNode<T>> _rootNodes = new Dictionary<string, TreeNode<T>>();

        public IEnumerable<TreeNode<T>> RootNodes
        {
            get
            {
                return _rootNodes.Values.ToArray();
            }
        }

        public bool TryAdd(string id, T value, bool override_if_exists)
        {
            if (_rootNodes.ContainsKey(id))
            {
                if (override_if_exists)
                {
                    (_rootNodes[id].Value as IDisposable)?.Dispose();
                    _rootNodes[id] = new TreeNode<T> { Value = value };
                    return true;
                }
                return false;
            }
            _rootNodes[id] = new TreeNode<T> { Value = value };
            return true;
        }

        private bool __TryAdd(string[] path, int index, T value, bool override_if_exists)
        {
            /* if (_rootNodes.ContainsKey(path[index]))
             {
             }*/
            return false;
        }

        public T Find(string[] path)
        {
            return default(T);
        }

        public bool TryAdd(string[] path, T value, bool override_if_exists)
        {
            if (path.Length == 0) return false;
            return __TryAdd(path, 0, value, override_if_exists);
        }

        public bool TryRemove(string id)
        {
            if (_rootNodes.ContainsKey(id))
            {
                (_rootNodes[id].Value as IDisposable)?.Dispose();
                _rootNodes[id].SubNodes?.Clear();
                _rootNodes.Remove(id);
                return true;
            }
            return false;
        }

        public void Clear()
        {
            foreach (var key in _rootNodes.Keys)
            {
                (_rootNodes[key].Value as IDisposable)?.Dispose();
                _rootNodes[key].SubNodes?.Clear();
            }
            _rootNodes.Clear();
        }
    }

    public class TreeNode<T>
    {
        public string Id { get; set; }
        public T Value { get; set; }
        public Tree<T> SubNodes = null;
    }
}