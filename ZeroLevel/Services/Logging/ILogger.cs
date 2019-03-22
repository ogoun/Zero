using System;

namespace ZeroLevel.Services.Logging
{
    public interface ILogger : IDisposable
    {
        void Write(LogLevel level, string message);
    }
}
