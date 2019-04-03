namespace ZeroLevel.Services.Logging
{
    public interface ILogComposer
    {
        void AddLogger(ILogger logger, LogLevel level = LogLevel.All);

        void SetupBacklog(long backlog);
    }
}