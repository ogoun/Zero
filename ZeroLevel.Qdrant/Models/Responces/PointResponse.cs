namespace ZeroLevel.Qdrant.Models.Responces
{
    public sealed class PointResponse
    {
        public Point[] result { get; set; }
        public string status { get; set; }
        public float time { get; set; }
    }
}
