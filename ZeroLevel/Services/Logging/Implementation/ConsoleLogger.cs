using System;

namespace ZeroLevel.Services.Logging.Implementation
{
    public sealed class ConsoleLogger
        : ILogger
    {
        public void Dispose()
        {
        }

        public void Write(LogLevel level, string message)
        {
            if (level == LogLevel.Raw)
            {
                Console.WriteLine(message);
            }
            else
            {
                Console.WriteLine("[{0:dd'.'MM'.'yyyy HH':'mm':'ss} {1}] {2}",
                    DateTime.Now, LogLevelNameMapping.CompactName(level), message);
            }
        }
    }
}