using Grpc.Net.Client;
using static Qdrant.Collections;

namespace Qdrant.Test
{
    // QDRANT VERSION 1.15.1
    internal class Program
    {
        const string COLLECTION_NAME = "my_test_collection";
        static void Main(string[] args)
        {
            var address = @"http://localhost:6334";
            var channel = GrpcChannel.ForAddress(address);
            var collections = new CollectionsClient(channel);
            var response = collections.Create(new CreateCollection
            {
                CollectionName = COLLECTION_NAME,
                VectorsConfig = new VectorsConfig
                {
                    Params = new VectorParams
                    {
                        Distance = Distance.Dot,
                        Size = 32,
                        HnswConfig = new HnswConfigDiff
                        {
                            OnDisk = false
                        }
                    }
                }
            });

            Console.WriteLine($"CREATED: {response.Result}");

            var d_response = collections.Delete(new DeleteCollection
            {
                CollectionName = COLLECTION_NAME
            });
            Console.WriteLine($"DELETED: {d_response.Result}");
        }
    }
}