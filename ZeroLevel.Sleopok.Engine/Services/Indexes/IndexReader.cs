using System.Collections.Generic;
using System.Threading.Tasks;
using ZeroLevel.Sleopok.Engine.Models;
using ZeroLevel.Sleopok.Engine.Services.Storage;

namespace ZeroLevel.Sleopok.Engine.Services.Indexes
{
    internal sealed class IndexReader<T>
        : IIndexReader<T>
    {
        private readonly DataStorage _storage;
        private readonly IndexInfo<T> _indexInfo;
        public IndexReader(DataStorage storage, IndexInfo<T> indexInfo)
        {
            _storage = storage;
            _indexInfo = indexInfo;
        }

        public async Task<Dictionary<string, float>> Search(string[] tokens, bool exactMatch)
        {
            var documents = new Dictionary<string, float>();

            foreach (var field in _indexInfo.Fields)
            {
                if (exactMatch && field.ExactMatch == false) 
                    continue;
                var docs = await _storage.GetDocuments(field.Name, tokens, field.Boost, exactMatch);
                foreach (var doc in docs)
                {
                    if (doc.Value > 0.0001f)
                    {
                        if (documents.ContainsKey(doc.Key) == false)
                        {
                            documents[doc.Key] = doc.Value;
                        }
                        else
                        {
                            documents[doc.Key] += doc.Value;
                        }
                    }
                }
            }
            return documents;
        }
    }
}
