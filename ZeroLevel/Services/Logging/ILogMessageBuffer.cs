using System;

namespace ZeroLevel.Services.Logging
{
    /// <summary>
    /// Message queue for logging
    /// </summary>
    internal interface ILogMessageBuffer : IDisposable
    {
        /// <summary>
        /// Number of messages in the queue
        /// </summary>
        long Count { get; }

        /// <summary>
        /// Write message to the queue
        /// </summary>
        void Push(LogLevel level, string message);

        Tuple<LogLevel, string> Take();
    }
}