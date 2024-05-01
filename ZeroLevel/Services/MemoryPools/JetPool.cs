using MemoryPools.Memory;
using System.Runtime.CompilerServices;

/*https://github.com/sidristij/memory-pools*/

namespace MemoryPools
{
    public class JetPool<T> where T : class, new()
    {
        private readonly JetStack<T> _freeObjectsQueue = new JetStack<T>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get() => _freeObjectsQueue.Count > 0 ? _freeObjectsQueue.Pop() : new T();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Return(T instance) => _freeObjectsQueue.Push(instance);
    }

    public class JetValPool<T>
    {
        private readonly JetStack<T> _freeObjectsQueue = new JetStack<T>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get() => (_freeObjectsQueue.Count > 0 ? _freeObjectsQueue.Pop() : default(T))!;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Return(T instance) => _freeObjectsQueue.Push(instance);
    }
}
