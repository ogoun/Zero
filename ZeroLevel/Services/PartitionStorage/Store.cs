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
        IStore<TKey, TInput, TValue, TMeta>, IDisposable
    {
        private readonly StoreOptions<TKey, TInput, TValue, TMeta> _options;
        private readonly IStoreSerializer<TKey, TInput, TValue> _serializer;
        private readonly PhisicalFileAccessorCachee _fileAccessorCachee;

        public Store(StoreOptions<TKey, TInput, TValue, TMeta> options,
            IStoreSerializer<TKey, TInput, TValue> serializer)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (serializer == null) throw new ArgumentNullException(nameof(serializer));
            _options = options;
            _serializer = serializer;
            if (Directory.Exists(_options.RootFolder) == false)
            {
                Directory.CreateDirectory(_options.RootFolder);
            }
            _fileAccessorCachee = new PhisicalFileAccessorCachee(options.PhisicalFileAccessorExpirationPeriod, TimeSpan.FromHours(2));
        }

        public void RemovePartition(TMeta info)
        {
            var partition = CreateAccessor(info);
            if (partition != null)
            {
                string path;
                using (partition)
                {
                    path = partition.GetCatalogPath();
                    partition.DropData();
                }
                FSUtils.RemoveFolder(path);
            }
        }

        public IStorePartitionAccessor<TKey, TInput, TValue> CreateAccessor(TMeta info)
        {
            if (false == Directory.Exists(_options.GetCatalogPath(info)))
            {
                return null;
            }
            return new StorePartitionAccessor<TKey, TInput, TValue, TMeta>(_options, info, _serializer, _fileAccessorCachee);
        }

        public IStorePartitionBuilder<TKey, TInput, TValue> CreateBuilder(TMeta info)
        {
            return new StorePartitionBuilder<TKey, TInput, TValue, TMeta>(_options, info, _serializer, _fileAccessorCachee);
        }

        public IStorePartitionMergeBuilder<TKey, TInput, TValue> CreateMergeAccessor(TMeta info, Func<TValue, IEnumerable<TInput>> decompressor)
        {
            return new StoreMergePartitionAccessor<TKey, TInput, TValue, TMeta>(_options, info, decompressor, _serializer, _fileAccessorCachee);
        }

        public void DropCache()
        {
            _fileAccessorCachee.DropAllDataReaders();
            _fileAccessorCachee.DropAllIndexReaders();
        }

        public async Task<StoreSearchResult<TKey, TValue, TMeta>> Search(StoreSearchRequest<TKey, TMeta> searchRequest)
        {
            var result = new StoreSearchResult<TKey, TValue, TMeta>();
            var results = new ConcurrentDictionary<TMeta, IEnumerable<KV<TKey, TValue>>>();
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
                    var accessor = CreateAccessor(pair.Key);
                    if (accessor != null)
                    {
                        using (accessor)
                        {
                            var set = new List<KV<TKey, TValue>>();
                            await foreach (var kv in accessor.Iterate())
                            {
                                set.Add(new KV<TKey, TValue>(kv.Key, kv.Value));
                            }
                            results[pair.Key] = set;
                        }
                    }
                });
            }
            result.Results = results;
            return result;
        }

        public void Dispose()
        {
            _fileAccessorCachee.Dispose();
        }

        public async IAsyncEnumerable<KV<TKey, TValue>> Bypass(TMeta meta)
        {
            var accessor = CreateAccessor(meta);
            if (accessor != null)
            {
                using (accessor)
                {
                    await foreach (var kv in accessor.Iterate())
                    {
                        yield return kv;
                    }
                }
            }
        }

        public async Task<bool> Exists(TMeta meta, TKey key)
        {
            var accessor = CreateAccessor(meta);
            if (accessor != null)
            {
                using (accessor)
                {
                    var info = await accessor.Find(key);
                    return info.Success;
                }
            }
            return false;
        }
    }
}
