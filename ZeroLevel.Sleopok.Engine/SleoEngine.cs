using System;
using ZeroLevel.Sleopok.Engine.Models;
using ZeroLevel.Sleopok.Engine.Services.Indexes;
using ZeroLevel.Sleopok.Engine.Services.Storage;

namespace ZeroLevel.Sleopok.Engine
{
    public class SleoEngine<T>
    {
        private readonly DataStorage _storage;
        private readonly IndexInfo<T> _indexInfo;
        public SleoEngine(string indexFolder, Func<T, string> identityExtractor)
        {
            _storage = new DataStorage(indexFolder);
            _indexInfo = new IndexInfo<T>(identityExtractor);
        }

        public bool HasData()
        {
            var total = 0;
            // healthy
            foreach (var field in _indexInfo.Fields)
            {
                var count = _storage.HasData(field.Name);
                Log.Debug($"Field: {field.Name}: {count} files");
                total += count;
            }
            return total > 0;
        }

        public IIndexBuilder<T> CreateBuilder()
        {
            return new IndexBuilder<T>(_storage, _indexInfo);
        }

        public IIndexReader<T> CreateReader()
        {
            return new IndexReader<T>(_storage, _indexInfo);
        }
    }
}
