namespace ZeroLevel.Qdrant.Models.Responces
{
    internal sealed class DeleteCollectionRequest
    {
        public DeleteCollectionRequest(string name) => delete_collection = name;
        public string delete_collection { get; set; }
    }
}
