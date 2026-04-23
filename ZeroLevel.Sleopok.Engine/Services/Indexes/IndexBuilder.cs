using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZeroLevel.Implementation.Semantic.Helpers;
using ZeroLevel.Sleopok.Engine.Models;
using ZeroLevel.Sleopok.Engine.Services.Storage;

namespace ZeroLevel.Sleopok.Engine.Services.Indexes
{
    internal sealed class IndexBuilder<T>
        : IIndexBuilder<T>
    {
        private readonly DataStorage _storage;
        private readonly IndexInfo<T> _indexInfo;
        private readonly Dictionary<string, IPartitionDataWriter> Indexers = new Dictionary<string, IPartitionDataWriter>();
        private bool _completed = false;
        public IndexBuilder(DataStorage storage, IndexInfo<T> indexInfo)
        {
            _storage = storage;
            _indexInfo = indexInfo;
            foreach (var field in indexInfo.Fields)
            {
                Indexers[field.Name] = _storage.GetWriter(field.Name);
            }
        }

        public async Task Complete()
        {
            if (_completed) return;
            _completed = true;
            foreach (var i in Indexers)
            {
                await i.Value.Complete();
                i.Value.Dispose();
            }
        }

        private static IEnumerable<string> Preprocess(string value)
        {
            if (string.IsNullOrWhiteSpace(value) == false)
            {
                return TextAnalizer.ExtractWords(value).Select(w=>w.ToLowerInvariant());
            }
            return Enumerable.Empty<string>();
        }

        public async Task Write(IEnumerable<T> batch)
        {
            foreach (var doc in batch)
            {
                var doc_id = _indexInfo.GetId(doc);
                foreach (var field in _indexInfo.Fields)
                {
                    if (field.FieldType == SleoFieldType.Array)
                    {
                        if (field.Getter(doc!) is System.Collections.IEnumerable items)
                        {
                            foreach (var item in items)
                            {
                                var value = item?.ToString();
                                if (string.IsNullOrWhiteSpace(value)) continue;
                                foreach (var t in Preprocess(value))
                                {
                                    await Indexers[field.Name].Write(t, doc_id);
                                }
                            }
                        }
                    }
                    else
                    {
                        var value = field.Getter(doc!)?.ToString() ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(value) == false)
                        {
                            foreach (var t in Preprocess(value))
                            {
                                await Indexers[field.Name].Write(t, doc_id);
                            }
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            if (_completed) return;
            Complete().GetAwaiter().GetResult();
        }
    }
}
