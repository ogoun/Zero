namespace ZeroLevel.Qdrant.Models.Responces
{
    public sealed class PointsOperationResult
    {
        public long operation_id { get; set; }
        public string status { get; set; }

    }

    public sealed class PointsOperationResponse
    {
        public PointsOperationResult result { get; set; }
        public string status { get; set; }
        public float time { get; set; }
    }
}
