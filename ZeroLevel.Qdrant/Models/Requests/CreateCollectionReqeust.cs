namespace ZeroLevel.Qdrant.Models.Requests
{
    internal sealed class CreateCollectionReqeust
    {
        public string distance { get; set; }
        public int vector_size { get; set; }
        public bool? on_disk_payload { get; set; }

        public CreateCollectionReqeust(string distance, int vector_size,
            bool? on_disk_payload = null)
        {
            this.distance = distance;
            this.vector_size = vector_size;
            this.on_disk_payload = on_disk_payload;
        }
    }
}
