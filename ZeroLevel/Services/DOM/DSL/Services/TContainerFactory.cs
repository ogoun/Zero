using System.Threading;
using ZeroLevel.Services.Pools;

namespace DOM.DSL.Services
{
    public class TContainerFactory
    {
        private readonly ObjectPool<TContainer> _pool;           

        private static int _get_count = 0;
        private static int _release_count = 0;

        internal TContainerFactory(TRender render)
        {
            _pool = new ObjectPool<TContainer>(() => new TContainer(this, render), 64);
        }

        internal TContainer Get(object value)
        {
            Interlocked.Increment(ref _get_count);
            var c = _pool.Allocate();
            c.Reset(value);
            return c;
        }
        internal TContainer Get(object value, int index)
        {
            Interlocked.Increment(ref _get_count);
            var c = _pool.Allocate();
            c.Reset(value);
            c.Index = index;
            return c;
        }
        internal void Release(TContainer container)
        {
            if (container != null)
            {
                Interlocked.Increment(ref _release_count);
                _pool.Free(container);
            }
        }

        public static int GetsCount() => _get_count;
        public static int ReleasesCount() => _release_count;
    }
}
