using System.Collections.Generic;

namespace ZeroLevel.Services.PartitionStorage
{
    public class StoreSearchResult<TKey, TValue, TMeta>
    {
        public IDictionary<TMeta, IEnumerable<KV<TKey, TValue>>> Results { get; set; }
    }
}
