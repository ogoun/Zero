using System;

namespace ZeroLevel.HNSW
{
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

        public static NSWOptions<float[]> Create(int v1, int v2, int v3, int v4, Func<float[], float[], float> l2Euclidean, object selectionHeuristic)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Distance function beetween vectors
        /// </summary>
        public readonly Func<TItem, TItem, float> Distance;

        public readonly int LayersCount;


        private NSWOptions(int layersCount,
            int m,
            int ef,
            int ef_construction,
            Func<TItem, TItem, float> distance)
        {
            LayersCount = layersCount;
            M = m;
            EF = ef;
            EFConstruction = ef_construction;
            Distance = distance;
        }

        public static NSWOptions<TItem> Create(int layersCount,
            int M,
            int EF,
            int EF_construction,
            Func<TItem, TItem, float> distance) =>
            new NSWOptions<TItem>(layersCount, M, EF, EF_construction, distance);
    }
}
