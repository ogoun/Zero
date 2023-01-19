using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZeroLevel.Services.FileSystem;
using ZeroLevel.Services.PartitionStorage.Interfaces;
using ZeroLevel.Services.PartitionStorage.Partition;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.PartitionStorage
{
    internal sealed class StorePartitionBuilder<TKey, TInput, TValue, TMeta>
        : BasePartition<TKey, TInput, TValue, TMeta>, IStorePartitionBuilder<TKey, TInput, TValue>
    {
        private readonly Func<TKey, TInput, bool> _storeMethod;

        private long _totalRecords = 0;

        public long TotalRecords { get { return _totalRecords; } }

        public StorePartitionBuilder(StoreOptions<TKey, TInput, TValue, TMeta> options,
            TMeta info,
            IStoreSerializer<TKey, TInput, TValue> serializer,
            PhisicalFileAccessorCachee fileAccessorCachee)
            : base(options, info, serializer, fileAccessorCachee)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (options.ThreadSafeWriting)
            {
                _storeMethod = StoreDirectSafe;
            }
            else
            {
                _storeMethod = StoreDirect;
            }
        }

        #region IStorePartitionBuilder


        public void Store(TKey key, TInput value)
        {
            if (_storeMethod.Invoke(key, value))
            {
                Interlocked.Increment(ref _totalRecords);
            }
        }

        public void CompleteAdding()
        {
            CloseWriteStreams();
        }

        public void Compress()
        {
            var files = Directory.GetFiles(_catalog);
            if (files != null && files.Length > 0)
            {
                Parallel.ForEach(files, file => CompressFile(file));
            }
        }
        public IEnumerable<StorePartitionKeyValueSearchResult<TKey, TInput>> Iterate()
        {
            var files = Directory.GetFiles(_catalog);
            if (files != null && files.Length > 0)
            {
                foreach (var file in files)
                {
                    var accessor = GetViewAccessor(file, 0);
                    if (accessor != null)
                    {
                        using (var reader = new MemoryStreamReader(accessor))
                        {
                            while (reader.EOS == false)
                            {
                                var key = Serializer.KeyDeserializer.Invoke(reader);
                                var val = Serializer.InputDeserializer.Invoke(reader);
                                yield return new StorePartitionKeyValueSearchResult<TKey, TInput> { Key = key, Value = val, Status = SearchResult.Success };
                            }
                        }
                    }
                }
            }
        }
        public void RebuildIndex() => RebuildIndexes();
        #endregion

        #region Private methods
        private bool StoreDirect(TKey key, TInput value)
        {
            var groupKey = _options.GetFileName(key, _info);
            if (TryGetWriteStream(groupKey, out var stream))
            {
                Serializer.KeySerializer.Invoke(stream, key);
                Thread.MemoryBarrier();
                Serializer.InputSerializer.Invoke(stream, value);
                return true;
            }
            else
            {
                Log.SystemError($"Fault create write stream for key '{groupKey}'");
            }
            return false;
        }
        private bool StoreDirectSafe(TKey key, TInput value)
        {
            var groupKey = _options.GetFileName(key, _info);
            bool lockTaken = false;
            if (TryGetWriteStream(groupKey, out var stream))
            {
                Monitor.Enter(stream, ref lockTaken);
                try
                {
                    Serializer.KeySerializer.Invoke(stream, key);
                    Thread.MemoryBarrier();
                    Serializer.InputSerializer.Invoke(stream, value);
                    return true;
                }
                finally
                {
                    if (lockTaken)
                    {
                        Monitor.Exit(stream);
                    }
                }
            }
            else
            {
                Log.SystemError($"Fault create write stream for key '{groupKey}'");
            }
            return false;
        }

        internal void CompressFile(string file)
        {
            var dict = new Dictionary<TKey, HashSet<TInput>>();
            var accessor = PhisicalFileAccessorCachee.GetDataAccessor(file, 0);
            if (accessor != null)
            {
                using (var reader = new MemoryStreamReader(accessor))
                {
                    while (reader.EOS == false)
                    {
                        var key = Serializer.KeyDeserializer.Invoke(reader);
                        if (false == dict.ContainsKey(key))
                        {
                            dict[key] = new HashSet<TInput>();
                        }
                        if (reader.EOS)
                        {
                            break;
                        }
                        var input = Serializer.InputDeserializer.Invoke(reader);
                        dict[key].Add(input);
                    }
                }
            }
            var tempFile = FSUtils.GetAppLocalTemporaryFile();
            using (var writer = new MemoryStreamWriter(new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096 * 1024)))
            {
                // sort for search acceleration
                foreach (var pair in dict.OrderBy(p => p.Key))
                {
                    var v = _options.MergeFunction(pair.Value);
                    writer.SerializeCompatible(pair.Key);
                    Thread.MemoryBarrier();
                    writer.SerializeCompatible(v);
                }
            }
            PhisicalFileAccessorCachee.DropDataReader(file);
            File.Delete(file);
            File.Move(tempFile, file, true);
        }
        #endregion
    }
}
