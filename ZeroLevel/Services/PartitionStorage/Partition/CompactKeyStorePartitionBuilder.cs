using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZeroLevel.Services.FileSystem;
using ZeroLevel.Services.PartitionStorage.Interfaces;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.PartitionStorage.Partition
{
    internal sealed class CompactKeyStorePartitionBuilder<TKey, TInput, TValue, TMeta>
         : BasePartition<TKey, TInput, TValue, TMeta>, IStorePartitionBuilder<TKey, TInput, TValue>
    {
        private readonly Action<TKey, TInput> _storeMethod;

        private long _totalRecords = 0;

        public long TotalRecords { get { return _totalRecords; } }

        public CompactKeyStorePartitionBuilder(StoreOptions<TKey, TInput, TValue, TMeta> options,
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
            _storeMethod.Invoke(key, value);
            Interlocked.Increment(ref _totalRecords);
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
            TKey key;
            TInput input;
            var files = Directory.GetFiles(_catalog);
            if (files != null && files.Length > 0)
            {
                foreach (var file in files)
                {
                    if (TryGetReadStream(file, out var reader))
                    {
                        using (reader)
                        {
                            while (reader.EOS == false)
                            {
                                if (Serializer.KeyDeserializer.Invoke(reader, out key) == false) break;
                                if (Serializer.InputDeserializer.Invoke(reader, out input) == false) break;
                                yield return new StorePartitionKeyValueSearchResult<TKey, TInput> { Key = key, Value = input, Status = SearchResult.Success };
                            }
                        }
                    }
                }
            }
        }
        public void RebuildIndex() => RebuildIndexes();
        #endregion

        #region Private methods
        private void StoreDirect(TKey key, TInput value)
        {
            var groupKey = _options.GetFileName(key, _info);
            if (TryGetWriteStream(groupKey, out var stream))
            {
                Serializer.KeySerializer.Invoke(stream, key);
                Thread.MemoryBarrier();
                Serializer.InputSerializer.Invoke(stream, value);
            }
        }
        private void StoreDirectSafe(TKey key, TInput value)
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
                }
                finally
                {
                    if (lockTaken)
                    {
                        Monitor.Exit(stream);
                    }
                }
            }
        }

        internal void CompressFile(string file)
        {
            TKey key;
            TInput input;
            var dict = new Dictionary<TKey, HashSet<TInput>>();
            PhisicalFileAccessorCachee.LockFile(file);
            try
            {
                using (var reader = new MemoryStreamReader(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None, 4096 * 1024)))
                {
                    while (reader.EOS == false)
                    {
                        if (false == Serializer.KeyDeserializer.Invoke(reader, out key))
                        {
                            throw new Exception($"[StorePartitionBuilder.CompressFile] Fault compress data in file '{file}'. Incorrect file structure. Fault read key.");
                        }
                        if (false == dict.ContainsKey(key))
                        {
                            dict[key] = new HashSet<TInput>();
                        }
                        if (reader.EOS)
                        {
                            break;
                        }
                        if (false == Serializer.InputDeserializer.Invoke(reader, out input))
                        {
                            throw new Exception($"[StorePartitionBuilder.CompressFile] Fault compress data in file '{file}'. Incorrect file structure. Fault input value.");
                        }
                        dict[key].Add(input);
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
                        writer.SerializeCompatible(v);
                    }
                }
                File.Delete(file);
                File.Move(tempFile, file, true);
            }
            finally
            {
                PhisicalFileAccessorCachee.UnlockFile(file);
            }
        }
        #endregion
    }
}
