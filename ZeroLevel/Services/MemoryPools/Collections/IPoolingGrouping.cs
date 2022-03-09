/*https://github.com/sidristij/memory-pools/blob/master/MemoryPools.Collections*/

namespace MemoryPools.Collections
{
    public interface IPoolingGrouping<out TKey, out TElement> : IPoolingEnumerable<TElement>
    {
        TKey Key { get; }
    }
}
