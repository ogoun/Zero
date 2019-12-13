using System;

namespace ZeroLevel.Logging
{
    public interface ILogger : IDisposable
    {
        void Write(LogLevel level, string message);
    }
}