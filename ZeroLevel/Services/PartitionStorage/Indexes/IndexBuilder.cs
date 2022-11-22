using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZeroLevel.Services.FileSystem;
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
        public IndexBuilder(IndexStepType indexType, int stepValue, string dataCatalog)
        {
            _dataCatalog = dataCatalog;
            _indexCatalog = Path.Combine(dataCatalog, INDEX_SUBFOLDER_NAME);
            _indexType = indexType;
            _stepValue = stepValue;
            _keyDeserializer = MessageSerializer.GetDeserializer<TKey>();
            _valueDeserializer = MessageSerializer.GetDeserializer<TValue>();
        }
        /// <summary>
        /// Rebuild indexes for all files
        /// </summary>
        internal void RebuildIndex()
        {
            FSUtils.CleanAndTestFolder(_indexCatalog);
            var files = Directory.GetFiles(_dataCatalog);
            if (files != null && files.Length > 0)
            {
                foreach (var file in files)
                {
                    RebuildFileIndex(file);
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
            if (File.Exists(index_file))
            {
                File.Delete(index_file);
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
            if (TryGetReadStream(file, out var reader))
            {
                using (reader)
                {
                    while (reader.EOS == false)
                    {
                        var pos = reader.Position;
                        var k = _keyDeserializer.Invoke(reader);
                        dict[k] = pos;
                        _valueDeserializer.Invoke(reader);
                    }
                }
            }
            if (dict.Count > _stepValue)
            {
                var step = (int)Math.Round(((float)dict.Count / (float)_stepValue), MidpointRounding.ToZero);
                var index_file = Path.Combine(_indexCatalog, Path.GetFileName(file));
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
            if (TryGetReadStream(file, out var reader))
            {
                using (reader)
                {
                    var index_file = Path.Combine(_indexCatalog, Path.GetFileName(file));
                    using (var writer = new MemoryStreamWriter(new FileStream(index_file, FileMode.Create, FileAccess.Write, FileShare.None)))
                    {
                        var counter = _stepValue;
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
            }
        }
        /// <summary>
        /// Attempting to open a file for reading
        /// </summary>
        private bool TryGetReadStream(string fileName, out MemoryStreamReader reader)
        {
            try
            {
                var filePath = Path.Combine(_dataCatalog, fileName);
                var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096 * 1024);
                reader = new MemoryStreamReader(stream);
                return true;
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, "[StorePartitionAccessor.TryGetReadStream]");
            }
            reader = null;
            return false;
        }
    }
}
