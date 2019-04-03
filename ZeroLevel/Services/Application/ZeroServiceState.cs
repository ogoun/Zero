using System;

namespace ZeroLevel.Services.Applications
{
    [Flags]
    public enum ZeroServiceState : int
    {
        Initialized = 0,
        Started = 1,
        Paused = 2,
        Stopped = 3
    }
}