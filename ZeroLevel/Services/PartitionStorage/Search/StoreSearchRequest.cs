using System.Collections.Generic;

namespace ZeroLevel.Services.PartitionStorage
{
    public class StoreSearchRequest<TKey, TMeta>
    {
        public IEnumerable<PartitionSearchRequest<TKey, TMeta>> PartitionSearchRequests { get; set; }
    }
}
