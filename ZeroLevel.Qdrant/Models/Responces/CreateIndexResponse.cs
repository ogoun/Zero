namespace ZeroLevel.Qdrant.Models.Responces
{
    public sealed class IndexOperation
    {
        public long operation_id { get; set; }
        public string status { get; set; }
    }

    public sealed class CreateIndexResponse
    {
        public IndexOperation result { get; set; }
        public string status { get; set; }
        public float time { get; set; }
    }
}
