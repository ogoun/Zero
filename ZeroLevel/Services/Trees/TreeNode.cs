using System;
using System.Collections.Generic;
using System.Linq;

namespace ZeroLevel.Services.Trees
{
    public class TreeNode : ITreeNode
    {
        private readonly List<ITreeNode> _children = new List<ITreeNode>();
        public IEnumerable<ITreeNode> Children { get { return _children; } }
        public bool IsSelected { get; set; }
        public string Name { get; }
        public object Tag { get; }

        internal TreeNode(string name, object tag)
        {
            Name = name;
            Tag = tag;
        }

        public TreeNode(TreeNode other)
        {
            if (other == null!)
            {
                throw new ArgumentNullException(nameof(other));
            }
            this.Name = other.Name;
            this.Tag = other.Tag;
            this.IsSelected = other.IsSelected;
            this._children = other._children.Select(a => (ITreeNode)a.Clone()).ToList();
        }

        public ITreeNode AppendChild(string name, object tag)
        {
            var child = new TreeNode(name, tag);
            _children.Add(child);
            return child;
        }

        public object Clone()
        {
            return new TreeNode(this);
        }
    }
}