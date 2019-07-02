using System;

namespace ZeroLevel
{
    [Flags]
    public enum ZeroServiceStatus : int
    {
        Initialized = 0,
        Running = 1,
        Stopped = 2,
        Disposed = 6
    }
}