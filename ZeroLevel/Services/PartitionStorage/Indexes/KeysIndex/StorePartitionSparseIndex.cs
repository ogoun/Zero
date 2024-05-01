using System;
using System.Collections.Generic;
using System.IO;
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
            int position = 0;
            if (inOneGroup)
            {
                var index = GetFileIndex(keys[0]);
                for (int i = 0; i < keys.Length; i++)
                {
                    result[i] = BinarySearchInIndex(index, keys[i], ref position);
                }
            }
            else
            {
                for (int i = 0; i < keys.Length; i++)
                {
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
            }
        }

        public void RemoveCacheeItem(string name)
        {
            var file = Path.Combine(_indexFolder, name);
            if (_enableIndexInMemoryCachee)
            {
                _phisicalFileAccessorCachee.DropIndexReader(file);
            }
        }

        private KeyIndex<TKey>[] GetFileIndex(TKey key)
        {
            var indexName = _filePartition.FileNameExtractor.Invoke(key, _meta);
            var filePath = Path.Combine(_indexFolder, indexName);
            try
            {
                return ReadIndexesFromIndexFile(filePath);
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, "[StorePartitionSparseIndex.GetFileIndex] No cachee");
            }
            return null!;
        }

        private KeyIndex<TKey>[] ReadIndexesFromIndexFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                var list = new List<KeyIndex<TKey>>();
                var accessor = _enableIndexInMemoryCachee ? _phisicalFileAccessorCachee.GetIndexAccessor(filePath, 0) : new StreamVewAccessor(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None));
                using (var reader = new MemoryStreamReader(accessor))
                {
                    var index = new KeyIndex<TKey>();
                    if (reader.EOS == false)
                    {
                        index.Key = _keyDeserializer.Invoke(reader);
                    }
                    if (reader.EOS == false)
                    {
                        index.Offset = reader.ReadLong();
                    }
                    while (reader.EOS == false)
                    {
                        var k = _keyDeserializer.Invoke(reader);
                        var o = reader.ReadLong();
                        index.Length = (int)(o - index.Offset);
                        list.Add(index);
                        index = new KeyIndex<TKey> { Key = k, Offset = o };
                    }
                    if (index.Offset > 0)
                    {
                        index.Length = 0;
                        list.Add(index);
                    }
                }
                return list.ToArray();
            }
            return null!;
        }
    }
}
