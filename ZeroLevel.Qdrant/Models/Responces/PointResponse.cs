namespace ZeroLevel.Qdrant.Models.Responces
{
    public sealed class Point
    {
        public long id { get; set; }
        public dynamic payload;
        public double[] vector;
    }

    public sealed class PointResponse
    {
        public Point[] result { get; set; }
        public string status { get; set; }
        public float time { get; set; }
    }
}
