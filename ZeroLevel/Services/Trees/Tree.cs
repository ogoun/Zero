using System;
using System.Collections.Generic;
using System.Linq;

namespace ZeroLevel.Services.Trees
{
    public class Tree : ITree
    {
        public Tree()
        {
        }

        public Tree(ITree other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));
            this._rootNodes = other.RootNodes.Select(a => (ITreeNode)a.Clone()).ToList();
        }

        private readonly List<ITreeNode> _rootNodes = new List<ITreeNode>();

        public IEnumerable<ITreeNode> RootNodes
        {
            get
            {
                return _rootNodes;
            }
        }

        public ITreeNode Append(string name, object tag)
        {
            var root = new TreeNode(name, tag);
            _rootNodes.Add(root);
            return root;
        }

        public static ITree Create()
        {
            return new Tree();
        }

        public static ITree Create(ITree other)
        {
            return new Tree(other);
        }
    }
}