namespace ZeroLevel.Qdrant.Models.Requests
{
    internal sealed class CreateIndexRequest
    {
        public CreateIndexRequest(string name) => create_index = name;
        public string create_index { get; set; }
    }
}
