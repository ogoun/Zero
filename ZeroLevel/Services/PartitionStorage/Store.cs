using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ZeroLevel.Services.FileSystem;
using ZeroLevel.Services.PartitionStorage.Interfaces;

namespace ZeroLevel.Services.PartitionStorage
{
    public class Store<TKey, TInput, TValue, TMeta> :
        IStore<TKey, TInput, TValue, TMeta>
    {
        private readonly StoreOptions<TKey, TInput, TValue, TMeta> _options;
        public Store(StoreOptions<TKey, TInput, TValue, TMeta> options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            _options = options;
            if (Directory.Exists(_options.RootFolder) == false)
            {
                Directory.CreateDirectory(_options.RootFolder);
            }
        }

        public void RemovePartition(TMeta info)
        {
            var partition = CreateAccessor(info);
            partition.DropData();
            FSUtils.RemoveFolder(partition.GetCatalogPath());
        }

        public IStorePartitionAccessor<TKey, TInput, TValue> CreateAccessor(TMeta info)
        {
            return new StorePartitionAccessor<TKey, TInput, TValue, TMeta>(_options, info);
        }

        public IStorePartitionBuilder<TKey, TInput, TValue> CreateBuilder(TMeta info)
        {
             return new StorePartitionBuilder<TKey, TInput, TValue, TMeta>(_options, info);
        }

        public IStorePartitionMergeBuilder<TKey, TInput, TValue> CreateMergeAccessor(TMeta info, Func<TValue, IEnumerable<TInput>> decompressor)
        {
            return new StoreMergePartitionAccessor<TKey, TInput, TValue, TMeta>(_options, info, decompressor);
        }

        public async Task<StoreSearchResult<TKey, TValue, TMeta>> Search(StoreSearchRequest<TKey, TMeta> searchRequest)
        {
            var result = new StoreSearchResult<TKey, TValue, TMeta>();
            var results = new ConcurrentDictionary<TMeta, IEnumerable<StorePartitionKeyValueSearchResult<TKey, TValue>>>();
            if (searchRequest.PartitionSearchRequests?.Any() ?? false)
            {
                var partitionsSearchInfo = searchRequest
                    .PartitionSearchRequests
                    .ToDictionary(r => r.Info, r => r.Keys);
                var options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism
                };
                await Parallel.ForEachAsync(partitionsSearchInfo, options, async (pair, _) =>
                {
                    using (var accessor = CreateAccessor(pair.Key))
                    {
                        results[pair.Key] = accessor
                            .Find(pair.Value)
                            .ToArray();
                    }
                });
            }
            result.Results = results;
            return result;
        }
    }
}
