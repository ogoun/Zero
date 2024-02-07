namespace ZeroLevel.HNSW.PHNSW
{
    public interface IPHNSWLevel<TPayload>
    {
        void Add(IPHNSWLevel<TPayload> prevLayer, Node<TPayload> node);
        Node<TPayload> Node { get; internal set; }
    }
}
