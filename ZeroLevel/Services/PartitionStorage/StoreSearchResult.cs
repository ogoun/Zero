using System.Collections.Generic;

namespace ZeroLevel.Services.PartitionStorage
{
    public class StoreSearchResult<TKey, TValue, TMeta>
    {
        public IDictionary<TMeta, IEnumerable<StorePartitionKeyValueSearchResult<TKey, TValue>>> Results { get; set; }
    }
}
