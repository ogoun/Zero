using System.Collections.Generic;
using System.Threading.Tasks;

namespace ZeroLevel.Services.Storages.PartitionFileSystemStorage
{
    public interface IPartitionFileStorage<TKey, TRecord>
    {
        Task WriteAsync(TKey key, IEnumerable<TRecord> records);
        Task<IEnumerable<TRecord>> CollectAsync(IEnumerable<TKey> keys);
        void Drop(TKey key);
    }
}
