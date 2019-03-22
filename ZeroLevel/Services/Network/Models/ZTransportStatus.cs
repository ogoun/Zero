namespace ZeroLevel.Services.Network
{
    public enum ZTransportStatus
        : int
    {
        Initialized = 0,
        Working = 1,
        Broken = 2,
        Disposed = 4
    }
}
