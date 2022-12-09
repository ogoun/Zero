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
        /// <summary>
        /// Список номеров которые разрешены к добавлению итогового результата, если поиск ведется в ограниченном наборе точек (например, после предварительной фильтрации)
        /// </summary>
        private HashSet<int> _activeNodes;
        /// <summary>
        /// Список точек с которых начинается поиск в графе для расширения
        /// </summary>
        private HashSet<int> _entryNodes;
        /// <summary>
        /// Режим работы алгоритма расширения, зависящий от того заданы ли ограничения в точках, и заданы ли точки начала поиска
        /// </summary>
        private Mode _mode;

        public Mode NodeCheckMode => _mode;
        public double PercentInTotal { get; private set; } = 0;
        public long AvaliableNodesCount => _activeNodes?.Count ?? 0;

        public SearchContext()
        {
            _mode = Mode.None;
        }

        /// <summary>
        /// Расчет процентного содержания точек доступных для использования в данном контексте, по отношению к общему количеству точек
        /// </summary>
        public SearchContext CaclulatePercentage(long total)
        {
            if ((_mode == Mode.ActiveCheck || _mode == Mode.ActiveInactiveCheck) && total > 0)
            {
                PercentInTotal = ((_activeNodes?.Count ?? 0 * 100d) / (double)total) / 100.0d;
            }
            return this;
        }

        public SearchContext SetPercentage(double percent)
        {
            PercentInTotal = percent;
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool _isActiveNode(int nodeId) => _activeNodes?.Contains(nodeId) ?? false;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool _isEntryNode(int nodeId) => _entryNodes?.Contains(nodeId) ?? false;


        /// <summary>
        /// Проверка, подходит ли указанная точка для включения в набор расширения
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsActiveNode(int nodeId)
        {
            switch (_mode)
            {
                // Если задан набор разрешенных к использованию точек, проверяется вхождение в него
                case Mode.ActiveCheck: return _isActiveNode(nodeId);
                // Если задан набор точек начала поиска, проверка невхождения точки в него
                case Mode.InactiveCheck: return _isEntryNode(nodeId) == false;
                // Если задан и ограничивающий и начальный наборы точек, проверка и на ограничение и на невхождение в начальный набор
                case Mode.ActiveInactiveCheck: return false == _isEntryNode(nodeId) && _isActiveNode(nodeId);
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
