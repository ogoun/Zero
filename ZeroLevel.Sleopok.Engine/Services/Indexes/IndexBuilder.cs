using System.Collections.Generic;
using System.Threading.Tasks;
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
            foreach (var i in Indexers)
            {
                await i.Value.Complete();
                i.Value.Dispose();
            }
        }

        public async Task Write(IEnumerable<T> batch)
        {
            foreach (var doc in batch)
            {
                var doc_id = _indexInfo.GetId(doc);
                foreach (var field in _indexInfo.Fields)
                {
                    var value = field.Getter(doc!)?.ToString() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(value) == false)
                    {
                        foreach (var t in value.Split(' '))
                        {
                            await Indexers[field.Name].Write(t, doc_id);
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            Complete().Wait();
        }
    }
}
