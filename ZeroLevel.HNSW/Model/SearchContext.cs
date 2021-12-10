﻿using System;
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
        private HashSet<int> _entryNodes;
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
