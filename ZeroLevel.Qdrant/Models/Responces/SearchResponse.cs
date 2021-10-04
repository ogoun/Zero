namespace ZeroLevel.Qdrant.Models.Responces
{
    public sealed class ScoredPoint
    {
        public long id { get; set; }
        public double score { get; set; }
    }
    public sealed class SearchResponse
    {
        public ScoredPoint[] result { get; set; }
        public string status { get; set; }
        public float time { get; set; }
    }
}
