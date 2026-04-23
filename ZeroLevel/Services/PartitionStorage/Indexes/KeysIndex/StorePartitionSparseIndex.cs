using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using ZeroLevel.Services.Memory;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.PartitionStorage
{
    internal sealed class StorePartitionSparseIndex<TKey, TMeta>
        : IStorePartitionIndex<TKey>
    {
        private readonly StoreFilePartition<TKey, TMeta> _filePartition;
        private readonly Func<TKey, TKey, int> _keyComparer;
        private readonly string _indexFolder;
        private readonly bool _indexExists = false;
        private readonly bool _enableIndexInMemoryCachee;
        private readonly Func<MemoryStreamReader, TKey> _keyDeserializer;
        private readonly TMeta _meta;
        private readonly PhisicalFileAccessorCachee _phisicalFileAccessorCachee;

        // Parsed-index cache: avoids re-deserializing the entire index file on every Find.
        // Copy-on-write: readers do plain Dictionary.TryGetValue on the snapshot from
        // Volatile.Read; writers build a new dict under _parsedIndexCacheLock and publish
        // it via Volatile.Write. Only used when EnableIndexInMemoryCachee is true.
        private Dictionary<string, KeyIndex<TKey>[]> _parsedIndexCache;
        private readonly object _parsedIndexCacheLock = new object();

        public StorePartitionSparseIndex(string partitionFolder, TMeta meta,
            StoreFilePartition<TKey, TMeta> filePartition,
            Func<TKey, TKey, int> keyComparer,
            bool enableIndexInMemoryCachee,
            PhisicalFileAccessorCachee phisicalFileAccessorCachee)
        {
            _indexFolder = Path.Combine(partitionFolder, "__indexes__");
            _indexExists = Directory.Exists(_indexFolder);
            _meta = meta;
            _keyComparer = keyComparer;
            _filePartition = filePartition;
            _keyDeserializer = MessageSerializer.GetDeserializer<TKey>();
            _enableIndexInMemoryCachee = enableIndexInMemoryCachee;
            if (_enableIndexInMemoryCachee)
            {
                _phisicalFileAccessorCachee = phisicalFileAccessorCachee;
                _parsedIndexCache = new Dictionary<string, KeyIndex<TKey>[]>();
            }
        }

        public KeyIndex<TKey> GetOffset(TKey key)
        {
            if (_indexExists)
            {
                var index = GetFileIndex(key);
                int pos = 0;
                if (index != null && index.Length > 0)
                {
                    return BinarySearchInIndex(index, key, ref pos);
                }
            }
            return new KeyIndex<TKey> { Key = key, Offset = 0 };
        }

        public KeyIndex<TKey>[] GetOffset(TKey[] keys, bool inOneGroup)
        {
            var result = new KeyIndex<TKey>[keys.Length];
            if (inOneGroup)
            {
                // The shared 'position' optimization in BinarySearchInIndex assumes
                // ascending key order. Sort indirectly so we can keep the original
                // result ordering for the caller.
                var index = GetFileIndex(keys[0]);
                var ordered = new int[keys.Length];
                for (int i = 0; i < ordered.Length; i++) ordered[i] = i;
                Array.Sort(ordered, (a, b) => _keyComparer(keys[a], keys[b]));

                int position = 0;
                for (int i = 0; i < ordered.Length; i++)
                {
                    var origIndex = ordered[i];
                    result[origIndex] = BinarySearchInIndex(index, keys[origIndex], ref position);
                }
            }
            else
            {
                // Different files per key; each search must start from position 0.
                for (int i = 0; i < keys.Length; i++)
                {
                    int position = 0;
                    var index = GetFileIndex(keys[i]);
                    result[i] = BinarySearchInIndex(index, keys[i], ref position);
                }
            }
            return result;
        }

        private KeyIndex<TKey> BinarySearchInIndex(KeyIndex<TKey>[] index, TKey key, ref int position)
        {
            if (index == null || index.Length == 0)
            {
                return new KeyIndex<TKey> { Key = key, Offset = 0 };
            }
            int left = position;
            int right = index.Length - 1;
            int mid = 0;
            TKey test;
            while (left <= right)
            {
                mid = (int)Math.Floor((right + left) / 2d);
                test = index[mid].Key;
                var c = _keyComparer(test, key);
                if (c < 0)
                {
                    left = mid + 1;
                }
                else if (c > 0)
                {
                    right = mid - 1;
                }
                else
                {
                    break;
                }
            }
            position = mid;
            while (_keyComparer(index[position].Key, key) > 0 && position > 0) position--;
            return index[position];
        }

        public void ResetCachee()
        {
            if (_enableIndexInMemoryCachee)
            {
                if (Directory.Exists(_indexFolder))
                {
                    foreach (var file in Directory.GetFiles(_indexFolder))
                    {
                        _phisicalFileAccessorCachee.DropIndexReader(file);
                    }
                }
                // drop parsed-index cache atomically
                Volatile.Write(ref _parsedIndexCache, new Dictionary<string, KeyIndex<TKey>[]>());
            }
        }

        public void RemoveCacheeItem(string name)
        {
            var file = Path.Combine(_indexFolder, name);
            if (_enableIndexInMemoryCachee)
            {
                _phisicalFileAccessorCachee.DropIndexReader(file);
                // copy-on-write removal
                lock (_parsedIndexCacheLock)
                {
                    if (_parsedIndexCache.ContainsKey(name))
                    {
                        var copy = new Dictionary<string, KeyIndex<TKey>[]>(_parsedIndexCache);
                        copy.Remove(name);
                        Volatile.Write(ref _parsedIndexCache, copy);
                    }
                }
            }
        }

        private KeyIndex<TKey>[] GetFileIndex(TKey key)
        {
            var indexName = _filePartition.FileNameExtractor.Invoke(key, _meta);
            if (_enableIndexInMemoryCachee)
            {
                var snap = Volatile.Read(ref _parsedIndexCache);
                if (snap.TryGetValue(indexName, out var cached)) return cached;
            }
            var filePath = Path.Combine(_indexFolder, indexName);
            KeyIndex<TKey>[] parsed;
            try
            {
                parsed = ReadIndexesFromIndexFile(filePath);
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, "[StorePartitionSparseIndex.GetFileIndex] No cachee");
                return null!;
            }
            if (_enableIndexInMemoryCachee && parsed != null!)
            {
                // copy-on-write publish
                lock (_parsedIndexCacheLock)
                {
                    if (!_parsedIndexCache.ContainsKey(indexName))
                    {
                        var copy = new Dictionary<string, KeyIndex<TKey>[]>(_parsedIndexCache.Count + 1);
                        foreach (var kv in _parsedIndexCache) copy[kv.Key] = kv.Value;
                        copy[indexName] = parsed;
                        Volatile.Write(ref _parsedIndexCache, copy);
                    }
                }
            }
            return parsed;
        }

        private KeyIndex<TKey>[] ReadIndexesFromIndexFile(string filePath)
        {
            if (!File.Exists(filePath)) return null!;
            var accessor = _enableIndexInMemoryCachee
                ? _phisicalFileAccessorCachee.GetIndexAccessor(filePath, 0)
                : new StreamVewAccessor(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None));
            if (accessor == null!) return null!;
            var list = new List<KeyIndex<TKey>>();
            using (var reader = new MemoryStreamReader(accessor))
            {
                if (reader.EOS) return new KeyIndex<TKey>[0];

                var prevKey = _keyDeserializer.Invoke(reader);
                if (reader.EOS)
                    return new KeyIndex<TKey>[0]; // truncated index file — defensive
                var prevOffset = reader.ReadLong();

                while (reader.EOS == false)
                {
                    var k = _keyDeserializer.Invoke(reader);
                    if (reader.EOS) break; // truncated trailing key
                    var o = reader.ReadLong();
                    list.Add(new KeyIndex<TKey> { Key = prevKey, Offset = prevOffset, Length = (int)(o - prevOffset) });
                    prevKey = k;
                    prevOffset = o;
                }
                // always add the final entry (Length = 0 means "scan to EOF")
                list.Add(new KeyIndex<TKey> { Key = prevKey, Offset = prevOffset, Length = 0 });
            }
            return list.ToArray();
        }
    }
}
