using System;

namespace ZeroLevel.Services.Logging
{
    /// <summary>
    /// Enum contains possible types of messages to write to the log
    /// </summary>
    [Flags]
    public enum LogLevel : int
    {
        None = 0,
        /// <summary>
        /// Message
        /// </summary>
        Info = 1 << 0,
        /// <summary>
        /// Warning
        /// </summary>
        Warning = 1 << 1,
        /// <summary>
        /// Error
        /// </summary>
        Error = 1 << 2,
        /// <summary>
        /// Fatal
        /// </summary>
        Fatal = 1 << 3,
        /// <summary>
        /// Debug
        /// </summary>
        Debug = 1 << 4,
        /// <summary>
        /// LowLevel Debug
        /// </summary>
        Verbose = 1 << 5,
        /// <summary>
        /// Info | Warning | Error | Fatal
        /// </summary>
        Standart = Info | Warning | Error | Fatal,
        /// <summary>
        /// Message output as is, without date and logging level
        /// </summary>
        Raw = 1 << 6,
        /// <summary>
        /// Error | Fatal | Warning
        /// </summary>
        Problem = Error | Fatal | Warning,
        /// <summary>
        /// Info | Problem | Raw
        /// </summary>
        All = Info | Problem | Raw,
        /// <summary>
        /// All | Verbose | Debug
        /// </summary>
        FullDebug = All | Verbose | Debug,

        SystemInfo = 1 << 6,
        SystemWarning = 1 << 7,
        SystemError = 1 << 8,

        System = SystemInfo | SystemError | SystemWarning,

        FullStandart = Standart | System
    }
}
