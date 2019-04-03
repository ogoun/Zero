using System;
using System.Collections.Generic;

namespace ZeroLevel.Services.Trees
{
    public interface ITreeNode : ICloneable
    {
        string Name { get; }
        object Tag { get; }
        bool IsSelected { get; set; }
        IEnumerable<ITreeNode> Children { get; }

        ITreeNode AppendChild(string name, object tag);
    }
}