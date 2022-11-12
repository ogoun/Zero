using System.Threading.Tasks;

namespace ZeroLevel.Services.PartitionStorage
{
    /// <summary>
    /// Partition store interface
    /// </summary>
    /// <typeparam name="TKey">Record key</typeparam>
    /// <typeparam name="TInput">The value that is written in the stream</typeparam>
    /// <typeparam name="TValue">Value after compression of TInput values by duplicate keys (TInput list or similar)</typeparam>
    /// <typeparam name="TMeta">Meta information for partition search</typeparam>
    public interface IStore<TKey, TInput, TValue, TMeta>
    {
        IStorePartitionAccessor<TKey, TInput, TValue> CreateAccessor(TMeta info);

        Task<StoreSearchResult<TKey, TValue, TMeta>> Search(StoreSearchRequest<TKey, TMeta> searchRequest);
    }
}
