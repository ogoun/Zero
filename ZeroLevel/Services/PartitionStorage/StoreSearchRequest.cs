using System.Collections.Generic;

namespace ZeroLevel.Services.PartitionStorage
{
    public class PartitionSearchRequest<TKey, TMeta>
    {
        public TMeta Info { get; set; }
        public IEnumerable<TKey> Keys { get; set; }
    }
    public class StoreSearchRequest<TKey, TMeta>
    {
        public IEnumerable<PartitionSearchRequest<TKey, TMeta>> PartitionSearchRequests { get; set; }
    }
}
