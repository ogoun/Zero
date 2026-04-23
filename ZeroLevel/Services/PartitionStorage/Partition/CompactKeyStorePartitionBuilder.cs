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
        private readonly Func<TKey, TInput, Task> _storeMethod;

        private long _totalRecords = 0;

        public long TotalRecords { get { return _totalRecords; } }

        public CompactKeyStorePartitionBuilder(StoreOptions<TKey, TInput, TValue, TMeta> options,
            TMeta info,
            IStoreSerializer<TKey, TInput, TValue> serializer,
            PhisicalFileAccessorCachee fileAccessorCachee)
            : base(options, info, serializer, fileAccessorCachee)
        {
            if (options == null!) throw new ArgumentNullException(nameof(options));
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


        public async Task Store(TKey key, TInput value)
        {
            await _storeMethod.Invoke(key, value);
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
                var maxDop = Math.Max(1, _options.MaxDegreeOfParallelism);
                using (var semaphore = new SemaphoreSlim(maxDop))
                {
                    var tasks = new Task[files.Length];
                    for (int i = 0; i < files.Length; i++)
                    {
                        var file = files[i];
                        tasks[i] = Task.Run(async () =>
                        {
                            await semaphore.WaitAsync();
                            try
                            {
                                await CompressFile(file);
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, $"[CompactKeyStorePartitionBuilder.Compress] '{file}'");
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        });
                    }
                    Task.WaitAll(tasks);
                }
            }
        }
        public async IAsyncEnumerable<SearchResult<TKey, TInput>> Iterate()
        {
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
                                var kv = await Serializer.KeyDeserializer.Invoke(reader);
                                if (kv.Success == false) break;

                                var iv = await Serializer.InputDeserializer.Invoke(reader);
                                if (iv.Success == false) break;

                                yield return new SearchResult<TKey, TInput> { Key = kv.Value, Value = iv.Value, Success = true };
                            }
                        }
                    }
                }
            }
        }
        public async Task RebuildIndex() => await RebuildIndexes();
        #endregion

        #region Private methods

        private async Task StoreDirect(TKey key, TInput value)
        {
            var groupKey = _options.GetFileName(key, _info);
            await WriteStreamAction(groupKey, async stream =>
            {
                await Serializer.KeySerializer.Invoke(stream, key);
                await Serializer.InputSerializer.Invoke(stream, value);
            });
        }
        private async Task StoreDirectSafe(TKey key, TInput value)
        {
            var groupKey = _options.GetFileName(key, _info);
            await SafeWriteStreamAction(groupKey, async stream => 
            {
                await Serializer.KeySerializer.Invoke(stream, key);
                await Serializer.InputSerializer.Invoke(stream, value);
            });
        }

        private static readonly TimeSpan _fileLockWaitTimeout = TimeSpan.FromSeconds(30);

        internal async Task CompressFile(string file)
        {
            var dict = new Dictionary<TKey, HashSet<TInput>>();
            PhisicalFileAccessorCachee.LockFileAndWait(file, _fileLockWaitTimeout);
            string tempFile = null!;
            try
            {
                using (var reader = new MemoryStreamReader(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None, 4096 * 1024)))
                {
                    while (reader.EOS == false)
                    {
                        var kv = await Serializer.KeyDeserializer.Invoke(reader);
                        if (kv.Success == false)
                        {
                            throw new Exception($"[StorePartitionBuilder.CompressFile] Fault compress data in file '{file}'. Incorrect file structure. Fault read key.");
                        }
                        if (false == dict.ContainsKey(kv.Value))
                        {
                            dict[kv.Value] = new HashSet<TInput>();
                        }
                        if (reader.EOS)
                        {
                            break;
                        }
                        var iv = await Serializer.InputDeserializer.Invoke(reader);
                        if (iv.Success == false)
                        {
                            throw new Exception($"[StorePartitionBuilder.CompressFile] Fault compress data in file '{file}'. Incorrect file structure. Fault input value.");
                        }
                        dict[kv.Value].Add(iv.Value);
                    }
                }
                // tempFile colocated with target → guaranteed same volume so File.Replace works cross-platform
                tempFile = file + ".tmp";
                if (File.Exists(tempFile)) File.Delete(tempFile);
                using (var writer = new MemoryStreamWriter(new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096 * 1024)))
                {
                    // sort for search acceleration
                    foreach (var pair in dict.OrderBy(p => p.Key))
                    {
                        var v = _options.MergeFunction(pair.Value);
                        writer.SerializeCompatible(pair.Key!);
                        writer.SerializeCompatible(v!);
                    }
                }
                if (File.Exists(file))
                {
                    File.Replace(tempFile, file, destinationBackupFileName: null);
                }
                else
                {
                    File.Move(tempFile, file);
                }
                tempFile = null!;
            }
            finally
            {
                if (tempFile != null!)
                {
                    try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
                }
                PhisicalFileAccessorCachee.UnlockFile(file);
            }
        }

        public override void Release()
        {
            if (Directory.Exists(_catalog))
            {
                foreach (var file in Directory.GetFiles(_catalog))
                {
                    PhisicalFileAccessorCachee.DropDataReader(file);
                }
            }
        }
        #endregion
    }
}
