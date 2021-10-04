namespace ZeroLevel.Qdrant.Models.Requests
{
    internal sealed class DeletePoints
    {
        public long[] ids { get; set; }
    }
    internal sealed class DeletePointsRequest
    {
        public DeletePoints delete_points { get; set; }
    }
}
