using System;

namespace ZeroLevel
{
    [Flags]
    public enum ZeroServiceState : int
    {
        Initialized = 0,
        Started = 1,
        Stopped = 2
    }
}