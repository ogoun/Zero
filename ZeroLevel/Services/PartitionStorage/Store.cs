using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ZeroLevel.Services.PartitionStorage
{
    public class Store<TKey, TInput, TValue, TMeta> :
        IStore<TKey, TInput, TValue, TMeta>
    {
        private readonly IStoreOptions<TKey, TInput, TValue, TMeta> _options;
        public Store(IStoreOptions<TKey, TInput, TValue, TMeta> options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            _options = options;
            if (Directory.Exists(_options.RootFolder) == false)
            {
                Directory.CreateDirectory(_options.RootFolder);
            }
        }



        public IStorePartitionAccessor<TKey, TInput, TValue> CreateAccessor(TMeta info)
        {
            return new StorePartitionAccessor<TKey, TInput, TValue, TMeta>(_options, info);
        }

        public async Task<StoreSearchResult<TKey, TValue, TMeta>> Search(StoreSearchRequest<TKey, TMeta> searchRequest)
        {
            var result = new StoreSearchResult<TKey, TValue, TMeta>();
            var results = new ConcurrentDictionary<TMeta, IEnumerable<StorePartitionKeyValueSearchResult<TKey, TValue>>>();
            if (searchRequest.PartitionSearchRequests?.Any() ?? false)
            {
                var partitionsSearchInfo = searchRequest.PartitionSearchRequests.ToDictionary(r => r.Info, r => r.Keys);
                //var options = new ParallelOptions { MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism };
                var options = new ParallelOptions { MaxDegreeOfParallelism = 1 };
                await Parallel.ForEachAsync(partitionsSearchInfo, options, async (pair, _) =>
                {
                    using (var accessor = CreateAccessor(pair.Key))
                    {
                        results[pair.Key] = accessor.Find(pair.Value).ToArray();
                    }
                });
            }
            result.Results = results;
            return result;
        }
    }
}
