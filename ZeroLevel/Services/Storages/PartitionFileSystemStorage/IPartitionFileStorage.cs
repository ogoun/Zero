using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ZeroLevel.Services.Storages.PartitionFileSystemStorage
{
    public interface IPartitionFileStorage<TKey, TRecord>
    {
        Task WriteAsync(TKey key, IEnumerable<TRecord> records);
        Task<IEnumerable<TRecord>> CollectAsync(IEnumerable<TKey> keys, Func<TRecord, bool> filter = null);
        void Drop(TKey key);
    }
}
