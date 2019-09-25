using System;

namespace ZeroLevel.Services.AsService
{
    /// <summary>
    /// Allows the service to control the host while running
    /// </summary>
    public interface HostControl
    {
        /// <summary>
        /// Tells the Host that the service is still starting, which resets the
        /// timeout.
        /// </summary>
        void RequestAdditionalTime(TimeSpan timeRemaining);

        /// <summary>
        /// Stops the Host
        /// </summary>
        void Stop();

        /// <summary>
        /// Stops the Host, returning the specified exit code
        /// </summary>
        void Stop(ExitCode exitCode);
    }
}
