/*https://github.com/dotnet/aspnetcore/blob/main/src/ObjectPool*/

namespace MemoryPools
{
    /// <summary>
    /// A base type for <see cref="IPooledObjectPolicy{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of object which is being pooled.</typeparam>
    public abstract class PooledObjectPolicy<T> : IPooledObjectPolicy<T> where T : notnull
    {
        /// <inheritdoc />
        public abstract T Create();

        /// <inheritdoc />
        public abstract bool Return(T obj);
    }
}
