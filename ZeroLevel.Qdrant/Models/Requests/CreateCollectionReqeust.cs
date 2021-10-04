namespace ZeroLevel.Qdrant.Models.Requests
{
    internal sealed class CreateCollectionParameters
    {
        public string name { get; set; }
        public string distance { get; set; }
        public int vector_size { get; set; }
    }
    internal sealed class CreateCollectionReqeust
    {
        public CreateCollectionParameters create_collection { get; set; }

        public CreateCollectionReqeust(string name, string distance, int vector_size)
        {
            create_collection = new CreateCollectionParameters
            {
                name = name,
                distance = distance,
                vector_size = vector_size
            };
        }
    }
}
