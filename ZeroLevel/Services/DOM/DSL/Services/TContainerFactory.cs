using MemoryPools;
using System.Threading;

namespace DOM.DSL.Services
{
    public class TContainerFactory
    {
        private readonly DefaultObjectPool<TContainer> _pool;
        private readonly TRender _render;
        private static int _get_count = 0;
        private static int _release_count = 0;

        internal TContainerFactory(TRender render)
        {
            _render = render;
            _pool = new DefaultObjectPool<TContainer>(new DefaultPooledObjectPolicy<TContainer>());
        }

        internal TContainer Get(object value)
        {
            Interlocked.Increment(ref _get_count);
            var c = _pool.Get();
            c.Init(this, _render);
            c.Reset(value);
            return c;
        }

        internal TContainer Get(object value, int index)
        {
            Interlocked.Increment(ref _get_count);
            var c = _pool.Get();
            c.Init(this, _render);
            c.Reset(value);
            c.Index = index;
            return c;
        }

        internal void Release(TContainer container)
        {
            if (container != null)
            {
                Interlocked.Increment(ref _release_count);
                _pool.Return(container);
            }
        }

        public static int GetsCount() => _get_count;

        public static int ReleasesCount() => _release_count;
    }
}