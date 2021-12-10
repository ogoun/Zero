using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ZeroLevel.HNSW
{
    public sealed class SearchContext
    {
        enum Mode
        {
            None,
            ActiveCheck,
            InactiveCheck,
            ActiveInactiveCheck
        }

        private HashSet<int> _activeNodes;
        private HashSet<int> _inactiveNodes;
        private Mode _mode;

        public SearchContext()
        {
            _mode = Mode.None;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsActiveNode(int nodeId)
        {
            switch (_mode)
            {
                case Mode.ActiveCheck: return _activeNodes.Contains(nodeId);
                case Mode.InactiveCheck: return _inactiveNodes.Contains(nodeId) == false;
                case Mode.ActiveInactiveCheck: return _inactiveNodes.Contains(nodeId) == false && _activeNodes.Contains(nodeId);
            }
            return nodeId >= 0;
        }

        public SearchContext SetActiveNodes(IEnumerable<int> activeNodes)
        {
            if (activeNodes != null && activeNodes.Any())
            {
                if (_mode == Mode.ActiveCheck || _mode == Mode.ActiveInactiveCheck)
                {
                    throw new InvalidOperationException("Active nodes are already defined");
                }
                _activeNodes = new HashSet<int>(activeNodes);
                if (_mode == Mode.None)
                {
                    _mode = Mode.ActiveCheck;
                }
                else if (_mode == Mode.InactiveCheck)
                {
                    _mode = Mode.ActiveInactiveCheck;
                }
            }
            return this;
        }

        public SearchContext SetInactiveNodes(IEnumerable<int> inactiveNodes)
        {
            if (inactiveNodes != null && inactiveNodes.Any())
            {
                if (_mode == Mode.InactiveCheck || _mode == Mode.ActiveInactiveCheck)
                {
                    throw new InvalidOperationException("Inctive nodes are already defined");
                }
                _inactiveNodes = new HashSet<int>(inactiveNodes);
                if (_mode == Mode.None)
                {
                    _mode = Mode.InactiveCheck;
                }
                else if (_mode == Mode.ActiveCheck)
                {
                    _mode = Mode.ActiveInactiveCheck;
                }
            }
            return this;
        }
    }
}
