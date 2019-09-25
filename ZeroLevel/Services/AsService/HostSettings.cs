using System;

namespace ZeroLevel.Services.AsService
{
    public enum UnhandledExceptionPolicyCode
    {
        /// <summary>
        /// If an UnhandledException occurs, AsService will log an error and 
        /// stop the service
        /// </summary>
        LogErrorAndStopService = 0,
        /// <summary>
        /// If an UnhandledException occurs, AsService will log an error and 
        /// continue without stopping the service
        /// </summary>
        LogErrorOnly = 1,
        /// <summary>
        /// If an UnhandledException occurs, AsService will take no action. 
        /// It is assumed that the application will handle the UnhandledException itself.
        /// </summary>
        TakeNoAction = 2
    }
    /// <summary>
    ///   The settings that have been configured for the operating system service
    /// </summary>
    public interface HostSettings
    {
        /// <summary>
        ///   The name of the service
        /// </summary>
        string Name { get; }

        /// <summary>
        ///   The name of the service as it should be displayed in the service control manager
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        ///   The description of the service that is displayed in the service control manager
        /// </summary>
        string Description { get; }

        /// <summary>
        ///   The service instance name that should be used when the service is registered
        /// </summary>
        string InstanceName { get; }

        /// <summary>
        ///   Returns the Windows service name, including the instance name, which is registered with the SCM Example: myservice$bob
        /// </summary>
        /// <returns> </returns>
        string ServiceName { get; }

        /// <summary>
        ///   True if the service supports pause and continue
        /// </summary>
        bool CanPauseAndContinue { get; }

        /// <summary>
        ///   True if the service can handle the shutdown event
        /// </summary>
        bool CanShutdown { get; }

        /// <summary>
        /// True if the service handles session change events
        /// </summary>
        bool CanSessionChanged { get; }

        /// <summary>
        /// True if the service handles power change events
        /// </summary>
        bool CanHandlePowerEvent { get; }

        /// <summary>
        /// The amount of time to wait for the service to start before timing out. Default is 10 seconds.
        /// </summary>
        TimeSpan StartTimeOut { get; }

        /// <summary>
        /// The amount of time to wait for the service to stop before timing out. Default is 10 seconds.
        /// </summary>
        TimeSpan StopTimeOut { get; }

        /// <summary>
        /// A callback to provide visibility into exceptions while AsService is performing its
        /// own handling.
        /// </summary>
        Action<Exception> ExceptionCallback { get; }

        /// <summary>
        /// The policy that will be used when Topself detects an UnhandledException in the
        /// application. The default policy is to log an error and to stop the service.
        /// </summary>
        UnhandledExceptionPolicyCode UnhandledExceptionPolicy { get; }
    }
}
