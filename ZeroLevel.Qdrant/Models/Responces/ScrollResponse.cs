namespace ZeroLevel.Qdrant.Models.Responces
{
    public sealed class ScrollResult
    {
        public Point[] points { get; set; }
        public long? next_page_offset { get; set; }
    }

    public sealed class ScrollResponse
    {
        public ScrollResult result { get; set; }
        public string status { get; set; }
        public float time { get; set; }
    }
}
