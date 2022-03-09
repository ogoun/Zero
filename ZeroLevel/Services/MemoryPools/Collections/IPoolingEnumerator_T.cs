﻿/*https://github.com/sidristij/memory-pools/blob/master/MemoryPools.Collections*/

namespace MemoryPools.Collections
{
    public interface IPoolingEnumerator<out T> : IPoolingEnumerator
    {
        // <summary>Gets the element in the collection at the current position of the enumerator.</summary>
        /// <returns>The element in the collection at the current position of the enumerator.</returns>
        new T Current { get; }
    }
}
