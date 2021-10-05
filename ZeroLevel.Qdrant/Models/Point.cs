namespace ZeroLevel.Qdrant.Models
{
    public sealed class Point
    {
        public long id { get; set; }
        public dynamic payload;
        public double[] vector;
    }
}
