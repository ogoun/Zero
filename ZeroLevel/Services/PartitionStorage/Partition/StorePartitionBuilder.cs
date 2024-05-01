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
        private readonly Func<TKey, TInput, Task<bool>> _storeMethod;

        private long _totalRecords = 0;

        public long TotalRecords { get { return _totalRecords; } }

        public StorePartitionBuilder(StoreOptions<TKey, TInput, TValue, TMeta> options,
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
            if (await _storeMethod.Invoke(key, value))
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
                var tasks = new Task[files.Length];
                for (int i = 0; i < files.Length; i++)
                {
                    tasks[i] = Task.Factory.StartNew(f =>
                    {
                        var file = (string)f;
                        try
                        {
                            CompressFile(file).Wait();
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, $"[StorePartitionBuilder.Compress] '{file}'");
                        }
                    }, files[i]);
                }
                Task.WaitAll(tasks);
            }
        }
        public async IAsyncEnumerable<SearchResult<TKey, TInput>> Iterate()
        {
            var files = Directory.GetFiles(_catalog);
            if (files != null && files.Length > 0)
            {
                foreach (var file in files)
                {
                    var accessor = GetViewAccessor(file, 0);
                    if (accessor != null!)
                    {
                        using (var reader = new MemoryStreamReader(accessor))
                        {
                            while (reader.EOS == false)
                            {
                                var kv = await Serializer.KeyDeserializer.Invoke(reader);
                                if (kv.Success == false) break;

                                var vv = await Serializer.InputDeserializer.Invoke(reader);
                                if (vv.Success == false) break;

                                yield return new SearchResult<TKey, TInput> { Key = kv.Value, Value = vv.Value, Success = true };
                            }
                        }
                    }
                }
            }
        }
        public async Task RebuildIndex() => await RebuildIndexes();
        #endregion

        #region Private methods
        private async Task<bool> StoreDirect(TKey key, TInput value)
        {
            var groupKey = _options.GetFileName(key, _info);
            try
            {
                await WriteStreamAction(groupKey, async stream =>
                {
                    await Serializer.KeySerializer.Invoke(stream, key);
                    await Serializer.InputSerializer.Invoke(stream, value);
                });
                return true;
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[StoreDirect] Fault use writeStream for key '{groupKey}'");
                return false;
            }
        }
        private async Task<bool> StoreDirectSafe(TKey key, TInput value)
        {
            var groupKey = _options.GetFileName(key, _info);
            try
            {
                await SafeWriteStreamAction(groupKey, async stream =>
                {
                    await Serializer.KeySerializer.Invoke(stream, key);
                    await Serializer.InputSerializer.Invoke(stream, value);
                });
                return true;
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[StoreDirectSafe] Fault use writeStream for key '{groupKey}'");
                return false;
            }
        }

        internal async Task CompressFile(string file)
        {
            PhisicalFileAccessorCachee.LockFile(file);
            try
            {
                var dict = new Dictionary<TKey, HashSet<TInput>>();
                var accessor = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None, 1024 * 1024 * 32);
                if (accessor != null!)
                {
                    using (var reader = new MemoryStreamReader(accessor))
                    {
                        while (reader.EOS == false)
                        {
                            var kv = await Serializer.KeyDeserializer.Invoke(reader);
                            if (kv.Success == false)
                            {
                                throw new Exception($"[StorePartitionBuilder.CompressFile] Fault compress data in file '{file}'. Incorrect file structure. Fault read key.");
                            }
                            if (kv.Value != null!)
                            {
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
                                    throw new Exception($"[StorePartitionBuilder.CompressFile] Fault compress data in file '{file}'. Incorrect file structure. Fault read input value.");
                                }
                                dict[kv.Value].Add(iv.Value);
                            }
                            else
                            {
                                Log.SystemWarning($"[StorePartitionBuilder.CompressFile] Null-value key in file '{file}'");
                            }
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
                        await Serializer.KeySerializer.Invoke(writer, pair.Key);
                        Thread.MemoryBarrier();
                        await Serializer.ValueSerializer.Invoke(writer, v);
                    }
                }
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
                File.Copy(tempFile, file, true);
                File.Delete(tempFile);
            }
            finally
            {
                PhisicalFileAccessorCachee.UnlockFile(file);
            }
        }

        public override void Release()
        {
        }
        #endregion
    }
}
