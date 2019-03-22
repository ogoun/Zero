using System.Collections.Generic;

namespace ZeroLevel.Services.Trees
{
    public interface ITree
    {
        IEnumerable<ITreeNode> RootNodes { get; }
        ITreeNode Append(string name, object tag);
    }
}
