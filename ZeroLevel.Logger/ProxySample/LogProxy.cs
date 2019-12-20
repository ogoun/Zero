using System;
using System.Net;
using ZeroLevel.Logging;
using ZeroLevel.Network;

namespace ZeroLevel.Logger.ProxySample
{
    public class LogProxy
       : IDisposable
    {
        private readonly IExchange _exchange;
        public LogProxy(IPEndPoint endpoint)
        {
            _exchange = Bootstrap.CreateExchange();
            _exchange.RoutesStorage.Set("log.service", endpoint);
        }

        public void Dispose()
        {
            _exchange?.Dispose();
        }

        public bool SendLog(LogMessage message)
        {
            try
            {
                return _exchange.Send("log.service", "log", message);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[LogProxy] SendLog");
                return false;
            }
        }

        public bool SendLog(LogLevel level, string message) => SendLog(new LogMessage { Level = level, Message = message });
    }
}
