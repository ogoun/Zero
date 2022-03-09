﻿using System;

/*https://github.com/dotnet/aspnetcore/blob/main/src/ObjectPool*/

namespace MemoryPools
{
    /// <summary>
    /// The default <see cref="ObjectPoolProvider"/>.
    /// </summary>
    public class DefaultObjectPoolProvider 
        : ObjectPoolProvider
    {
        /// <summary>
        /// The maximum number of objects to retain in the pool.
        /// </summary>
        public int MaximumRetained { get; set; } = Environment.ProcessorCount * 2;

        /// <inheritdoc/>
        public override ObjectPool<T> Create<T>(IPooledObjectPolicy<T> policy)
        {
            if (policy == null)
            {
                throw new ArgumentNullException(nameof(policy));
            }

            if (typeof(IDisposable).IsAssignableFrom(typeof(T)))
            {
                return new DisposableObjectPool<T>(policy, MaximumRetained);
            }

            return new DefaultObjectPool<T>(policy, MaximumRetained);
        }
    }
}
