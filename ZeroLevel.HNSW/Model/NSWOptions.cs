using System;

namespace ZeroLevel.HNSW
{
    /// <summary>
    /// Type of heuristic to select best neighbours for a node.
    /// </summary>
    public enum NeighbourSelectionHeuristic
    {
        /// <summary>
        /// Marker for the Algorithm 3 (SELECT-NEIGHBORS-SIMPLE) from the article. Implemented in <see cref="Algorithms.Algorithm3{TItem, TDistance}"/>
        /// </summary>
        SelectSimple,

        /// <summary>
        /// Marker for the Algorithm 4 (SELECT-NEIGHBORS-HEURISTIC) from the article. Implemented in <see cref="Algorithms.Algorithm4{TItem, TDistance}"/>
        /// </summary>
        SelectHeuristic
    }

    public sealed class NSWOptions<TItem>
    {
        /// <summary>
        /// Mox node connections on Layer
        /// </summary>
        public readonly int M;

        /// <summary>
        /// Max search buffer
        /// </summary>
        public readonly int EF;
        /// <summary>
        /// Max search buffer for inserting
        /// </summary>
        public readonly int EFConstruction;
        /// <summary>
        /// Distance function beetween vectors
        /// </summary>
        public readonly Func<TItem, TItem, float> Distance;

        public readonly bool ExpandBestSelection;

        public readonly bool KeepPrunedConnections;

        public readonly NeighbourSelectionHeuristic SelectionHeuristic;

        public readonly int LayersCount;


        private NSWOptions(int layersCount,
            int m,
            int ef,
            int ef_construction,
            Func<TItem, TItem, float> distance,
            bool expandBestSelection,
            bool keepPrunedConnections,
            NeighbourSelectionHeuristic selectionHeuristic)
        {
            LayersCount = layersCount;
            M = m;
            EF = ef;
            EFConstruction = ef_construction;
            Distance = distance;
            ExpandBestSelection = expandBestSelection;
            KeepPrunedConnections = keepPrunedConnections;
            SelectionHeuristic = selectionHeuristic;
        }

        public static NSWOptions<TItem> Create(int layersCount,
            int M,
            int EF,
            int EF_construction,
            Func<TItem, TItem, float> distance,
            bool expandBestSelection = false,
            bool keepPrunedConnections = false,
            NeighbourSelectionHeuristic selectionHeuristic = NeighbourSelectionHeuristic.SelectSimple) =>
            new NSWOptions<TItem>(layersCount, M, EF, EF_construction, distance, expandBestSelection, keepPrunedConnections, selectionHeuristic);
    }
}
