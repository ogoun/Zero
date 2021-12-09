using System;

namespace ZeroLevel.HNSW
{
    public sealed class NSWReadOnlyOption<TItem>
    {
        /// <summary>
        /// Max search buffer
        /// </summary>
        public readonly int EF;
        /// <summary>
        /// Distance function beetween vectors
        /// </summary>
        public readonly Func<TItem, TItem, float> Distance;

        public readonly bool ExpandBestSelection;

        public readonly bool KeepPrunedConnections;

        public readonly NeighbourSelectionHeuristic SelectionHeuristic;

        private NSWReadOnlyOption(
            int ef,
            Func<TItem, TItem, float> distance,
            bool expandBestSelection,
            bool keepPrunedConnections,
            NeighbourSelectionHeuristic selectionHeuristic)
        {
            EF = ef;
            Distance = distance;
            ExpandBestSelection = expandBestSelection;
            KeepPrunedConnections = keepPrunedConnections;
            SelectionHeuristic = selectionHeuristic;
        }

        public static NSWReadOnlyOption<TItem> Create(
            int EF,
            Func<TItem, TItem, float> distance,
            bool expandBestSelection = false,
            bool keepPrunedConnections = false,
            NeighbourSelectionHeuristic selectionHeuristic = NeighbourSelectionHeuristic.SelectSimple) =>
            new NSWReadOnlyOption<TItem>(EF, distance, expandBestSelection, keepPrunedConnections, selectionHeuristic);
    }
}
