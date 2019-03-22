using System;

namespace ZeroLevel.Services.Logging.Implementation
{
    public sealed class DelegateLogger : ILogger
    {
        private readonly Action<string> _handler;

        public DelegateLogger(Action<string> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public void Dispose() { }

        public void Write(LogLevel level, string message)
        {
            try
            {
                if (level == LogLevel.Raw)
                {
                    _handler(message);
                }
                else
                {
                    _handler($"[{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")} {LogLevelNameMapping.CompactName(level)}] {message}");
                }
            }
            catch
            { }
        }
    }
}
