using System;

namespace ZeroLevel.HNSW
{
    public class VisitedBitSet
    {
        // bit map
        private int[] Buffer;

        public VisitedBitSet(int nodesCount, int M)
        {
            Buffer = new int[(nodesCount >> 5) + M + 1];
        }

        public bool Contains(int nodeId)
        {
            int carrier = Buffer[nodeId >> 5];
            return ((1 << (nodeId & 31)) & carrier) != 0;
        }

        public void Add(int nodeId)
        {
            int mask = 1 << (nodeId & 31);
            Buffer[nodeId >> 5] |= mask;
        }

        public void Clear()
        {
            Array.Clear(Buffer, 0, Buffer.Length);
        }
    }
}
