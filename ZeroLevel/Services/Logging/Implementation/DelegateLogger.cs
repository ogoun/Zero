using System;

namespace ZeroLevel.Logging
{
    public sealed class DelegateLogger : ILogger
    {
        private readonly Action<string> _handler;
        private readonly Action<LogLevel, string> _handlerFull;

        public DelegateLogger(Action<string> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public DelegateLogger(Action<LogLevel, string> handler)
        {
            _handlerFull = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public void Dispose()
        {
        }

        public void Write(LogLevel level, string message)
        {
            try
            {
                if (level == LogLevel.Raw)
                {
                    _handler?.Invoke(message);
                    _handlerFull?.Invoke(level, message);
                }
                else
                {
                    _handler?.Invoke($"[{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")} {LogLevelNameMapping.CompactName(level)}] {message}");
                    _handlerFull?.Invoke(level, message);
                }
            }
            catch
            { }
        }
    }
}