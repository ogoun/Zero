using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ZeroLevel.HNSW
{
    public enum Mode
    {
        None,
        ActiveCheck,
        InactiveCheck,
        ActiveInactiveCheck
    }

    public sealed class SearchContext
    {
        private HashSet<int> _activeNodes;
        private HashSet<int> _entryNodes;
        private Mode _mode;

        public Mode NodeCheckMode => _mode;
        public double PercentInTotal { get; private set; } = 0;
        public long AvaliableNodesCount => _activeNodes?.Count ?? 0;

        public SearchContext()
        {
            _mode = Mode.None;
        }

        public SearchContext CaclulatePercentage(long total)
        {
            if (total > 0)
            {
                PercentInTotal = ((_activeNodes.Count * 100d) / (double)total) / 100.0d;
            }
            return this;
        }

        public SearchContext SetPercentage(double percent)
        {
            PercentInTotal = percent;
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsActiveNode(int nodeId)
        {
            switch (_mode)
            {
                case Mode.ActiveCheck: return _activeNodes.Contains(nodeId);
                case Mode.InactiveCheck: return _entryNodes.Contains(nodeId) == false;
                case Mode.ActiveInactiveCheck: return _entryNodes.Contains(nodeId) == false && _activeNodes.Contains(nodeId);
            }
            return nodeId >= 0;
        }

        public IEnumerable<int> EntryPoints => _entryNodes;

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

        public SearchContext SetEntryPointsNodes(IEnumerable<int> entryNodes)
        {
            if (entryNodes != null && entryNodes.Any())
            {
                if (_mode == Mode.InactiveCheck || _mode == Mode.ActiveInactiveCheck)
                {
                    throw new InvalidOperationException("Inctive nodes are already defined");
                }
                _entryNodes = new HashSet<int>(entryNodes);
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
