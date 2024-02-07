using ZeroLevel.DocumentObjectModel.Flow;

namespace ZeroLevel.HNSW.PHNSW
{
    public class Node <TPayload>
    {
        public float[] Vector { get; set; }
        public TPayload Payload { get; set; }

        public List<Node<TPayload>> Neighbors { get; }
    }
}
