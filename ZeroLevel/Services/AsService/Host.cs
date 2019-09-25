namespace ZeroLevel.Services.AsService
{
    public enum ExitCode
    {
        Ok = 0,
        ServiceAlreadyInstalled = 1242,
        ServiceNotInstalled = 1243,
        ServiceAlreadyRunning = 1056,
        ServiceNotRunning = 1062,
        ServiceControlRequestFailed = 1064,
        AbnormalExit = 1067,
        SudoRequired = 2,
        NotRunningOnWindows = 11
    }
    /// <summary>
    ///   A Host can be a number of configured service hosts, from installers to service runners
    /// </summary>
    public interface Host
    {
        /// <summary>
        ///   Runs the configured host
        /// </summary>
        ExitCode Run();
    }
}
