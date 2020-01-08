using System.Threading;

namespace TestApp
{
    public class HybrydLock
    {
        private AutoResetEvent _lock = new AutoResetEvent(false);
        private volatile int _counter = 0;

        public void Enter()
        {
            if (Interlocked.Increment(ref _counter) == 1)
            {
                return;
            }
            _lock.WaitOne();
        }

        public void Leave()
        {
            if (Interlocked.Decrement(ref _counter) == 0)
            {
                return;
            }
            _lock.Set();
        }
    }
}
