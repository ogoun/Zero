namespace ZeroLevel.Qdrant.Models.Requests
{
    internal sealed class PointsRequest
    {
        public long[] ids { get; set; }
        public bool with_payload { get; set; } = true;
        public bool with_vector { get; set; } = false;
    }
}
