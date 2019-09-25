using System;

namespace ZeroLevel.Services.AsService
{
    /// <summary>
    /// A handle to a service being hosted by the Host
    /// </summary>
    public interface ServiceHandle :
        IDisposable
    {
        /// <summary>
        /// Start the service
        /// </summary>
        /// <param name="hostControl"></param>
        /// <returns>True if the service was started, otherwise false</returns>
        bool Start(HostControl hostControl);

        /// <summary>
        /// Pause the service
        /// </summary>
        /// <param name="hostControl"></param>
        /// <returns>True if the service was paused, otherwise false</returns>
        bool Pause(HostControl hostControl);

        /// <summary>
        /// Continue the service from a paused state
        /// </summary>
        /// <param name="hostControl"></param>
        /// <returns>True if the service was able to continue, otherwise false</returns>
        bool Continue(HostControl hostControl);

        /// <summary>
        /// Stop the service
        /// </summary>
        /// <param name="hostControl"></param>
        /// <returns>True if the service was stopped, or false if the service cannot be stopped at this time</returns>
        bool Stop(HostControl hostControl);

        /// <summary>
        /// Handle the shutdown event
        /// </summary>
        /// <param name="hostControl"></param>
        void Shutdown(HostControl hostControl);

        /// <summary>
        /// Handle the session change event
        /// </summary>
        /// <param name="hostControl"></param>
        /// <param name="arguments"></param>
        void SessionChanged(HostControl hostControl, SessionChangedArguments arguments);

        /// <summary>
        /// Handle the power change event
        /// </summary>
        /// <param name="hostControl"></param>
        /// <param name="arguments"></param>
        bool PowerEvent(HostControl hostControl, PowerEventArguments arguments);

        /// <summary>
        /// Handle the custom command
        /// </summary>
        /// <param name="hostControl"></param>
        /// <param name="command"></param>
        void CustomCommand(HostControl hostControl, int command);
    }
}
