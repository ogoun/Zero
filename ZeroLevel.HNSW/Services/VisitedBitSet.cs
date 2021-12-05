using System;

namespace ZeroLevel.HNSW
{
    internal class VisitedBitSet
    {
        // bit map
        private int[] Buffer;

        internal VisitedBitSet(int nodesCount, int M)
        {
            Buffer = new int[(nodesCount >> 5) + M + 1];
        }

        internal bool Contains(int nodeId)
        {
            int carrier = Buffer[nodeId >> 5];
            return ((1 << (nodeId & 31)) & carrier) != 0;
        }

        internal void Add(int nodeId)
        {
            int mask = 1 << (nodeId & 31);
            Buffer[nodeId >> 5] |= mask;
        }

        internal void Clear()
        {
            Array.Clear(Buffer, 0, Buffer.Length);
        }
    }
}
