﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.PartitionStorage
{
    /// <summary>
    /// Responsible for building index files
    /// </summary>
    internal sealed class IndexBuilder<TKey, TValue>
    {
        private const string INDEX_SUBFOLDER_NAME = "__indexes__";
        private readonly IndexStepType _indexType;
        private readonly string _indexCatalog;
        private readonly string _dataCatalog;
        private readonly int _stepValue;
        private readonly Func<MemoryStreamReader, TKey> _keyDeserializer;
        private readonly Func<MemoryStreamReader, TValue> _valueDeserializer;
        private readonly PhisicalFileAccessorCachee _phisicalFileAccessorCachee;
        public IndexBuilder(IndexStepType indexType, int stepValue, string dataCatalog, PhisicalFileAccessorCachee phisicalFileAccessorCachee)
        {
            _dataCatalog = dataCatalog;
            _indexCatalog = Path.Combine(dataCatalog, INDEX_SUBFOLDER_NAME);
            _indexType = indexType;
            _stepValue = stepValue;
            _keyDeserializer = MessageSerializer.GetDeserializer<TKey>();
            _valueDeserializer = MessageSerializer.GetDeserializer<TValue>();
            _phisicalFileAccessorCachee = phisicalFileAccessorCachee;
        }
        /// <summary>
        /// Rebuild indexes for all files
        /// </summary>
        internal void RebuildIndex()
        {
            var files = Directory.GetFiles(_dataCatalog);
            if (files != null && files.Length > 0)
            {
                foreach (var file in files)
                {
                    RebuildFileIndex(Path.GetFileName(file));
                }
            }
        }
        /// <summary>
        /// Rebuild index for the specified file
        /// </summary>
        internal void RebuildFileIndex(string file)
        {
            if (_indexType == IndexStepType.AbsoluteCount)
            {
                RebuildFileIndexWithAbsoluteCountIndexes(file);
            }
            else
            {
                RebuildFileIndexWithSteps(file);
            }
        }

        /// <summary>
        /// Delete the index for the specified file
        /// </summary>
        internal void DropFileIndex(string file)
        {
            var index_file = Path.Combine(_indexCatalog, Path.GetFileName(file));
            _phisicalFileAccessorCachee.LockFile(index_file);
            try
            {
                if (File.Exists(index_file))
                {
                    File.Delete(index_file);
                }
            }
            finally 
            {
                _phisicalFileAccessorCachee.UnlockFile(index_file);
            }
        }

        /// <summary>
        /// Rebuild index with specified number of steps for specified file
        /// </summary>
        private void RebuildFileIndexWithAbsoluteCountIndexes(string file)
        {
            if (false == Directory.Exists(_indexCatalog))
            {
                Directory.CreateDirectory(_indexCatalog);
            }
            var dict = new Dictionary<TKey, long>();
            using (var reader = new MemoryStreamReader(new FileStream(Path.Combine(_dataCatalog, file), FileMode.Open, FileAccess.Read, FileShare.None)))
            {
                while (reader.EOS == false)
                {
                    var pos = reader.Position;
                    var k = _keyDeserializer.Invoke(reader);
                    dict[k] = pos;
                    _valueDeserializer.Invoke(reader);
                }
            }
            if (dict.Count > _stepValue)
            {
                var step = (int)Math.Round(dict.Count / (float)_stepValue, MidpointRounding.ToZero);
                var index_file = Path.Combine(_indexCatalog, Path.GetFileName(file));

                _phisicalFileAccessorCachee.LockFile(index_file);
                if (File.Exists(index_file))
                {
                    File.Delete(index_file);
                }
                try
                {
                    var d_arr = dict.OrderBy(p => p.Key).ToArray();
                    using (var writer = new MemoryStreamWriter(new FileStream(index_file, FileMode.Create, FileAccess.Write, FileShare.None)))
                    {
                        for (int i = 0; i < _stepValue; i++)
                        {
                            var pair = d_arr[i * step];
                            writer.WriteCompatible(pair.Key);
                            writer.WriteLong(pair.Value);
                        }
                    }
                }
                finally
                {
                    _phisicalFileAccessorCachee.UnlockFile(index_file);
                }
            }
        }
        /// <summary>
        /// Rebuild index with specified step for keys
        /// </summary>
        private void RebuildFileIndexWithSteps(string file)
        {
            if (false == Directory.Exists(_indexCatalog))
            {
                Directory.CreateDirectory(_indexCatalog);
            }
            using (var reader = new MemoryStreamReader(new FileStream(Path.Combine(_dataCatalog, file), FileMode.Open, FileAccess.Read, FileShare.None)))
            {
                var index_file = Path.Combine(_indexCatalog, Path.GetFileName(file));
                _phisicalFileAccessorCachee.LockFile(index_file);
                if (File.Exists(index_file))
                {
                    File.Delete(index_file);
                }
                try
                {
                    using (var writer = new MemoryStreamWriter(new FileStream(index_file, FileMode.Create, FileAccess.Write, FileShare.None)))
                    {
                        var counter = 1;
                        while (reader.EOS == false)
                        {
                            counter--;
                            var pos = reader.Position;
                            var k = _keyDeserializer.Invoke(reader);
                            _valueDeserializer.Invoke(reader);
                            if (counter == 0)
                            {
                                writer.WriteCompatible(k);
                                writer.WriteLong(pos);
                                counter = _stepValue;
                            }
                        }
                    }
                }
                finally
                {
                    _phisicalFileAccessorCachee.UnlockFile(index_file);
                }
            }
        }
    }
}
