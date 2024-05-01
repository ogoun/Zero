using ZeroLevel.Logger.ProxySample;
using ZeroLevel.Network;
using ZeroLevel.Services.Applications;

namespace ZeroLevel.Logger
{
    public class LogService
        : BaseZeroService
    {
        public LogService()
            : base()
        {
            var config = Configuration.Default;
            if (config.FirstOrDefault<bool>("useConsoleLog"))
            {
                Log.AddConsoleLogger(Logging.LogLevel.FullDebug);
            }
            if (config.FirstOrDefault<bool>("useFileLog"))
            {
                Log.AddTextFileLogger(new Logging.TextFileLoggerOptions().SetFolderPath("logs"));
            }
            AutoregisterInboxes(UseHost(config.First<int>("port")));
        }
        protected override void StartAction()
        {

        }

        protected override void StopAction()
        {

        }

        [ExchangeHandler("log")]
        public void LogMessageHandler(ISocketClient client, LogMessage message)
        {
            Log.Write(message.Level, message.Message);
        }
    }
}
