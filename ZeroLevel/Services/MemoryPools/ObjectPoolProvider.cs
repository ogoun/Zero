/*https://github.com/dotnet/aspnetcore/blob/main/src/ObjectPool*/

namespace MemoryPools
{
    /// <summary>
    /// A provider of <see cref="ObjectPool{T}"/> instances.
    /// </summary>
    public abstract class ObjectPoolProvider
    {
        /// <summary>
        /// Creates an <see cref="ObjectPool"/>.
        /// </summary>
        /// <typeparam name="T">The type to create a pool for.</typeparam>
        public ObjectPool<T> Create<T>() where T : class, new()
        {
            return Create<T>(new DefaultPooledObjectPolicy<T>());
        }

        /// <summary>
        /// Creates an <see cref="ObjectPool"/> with the given <see cref="IPooledObjectPolicy{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type to create a pool for.</typeparam>
        public abstract ObjectPool<T> Create<T>(IPooledObjectPolicy<T> policy) where T : class;
    }
}
