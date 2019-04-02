using System;

namespace ZeroLevel.Services.Logging
{
    public interface ILog
    {
        /// <summary>
        /// Message output as is, without adding a logging level and date
        /// </summary>
        void Raw(string line, params object[] args);
        /// <summary>
        /// Message
        /// </summary>
        void Info(string line, params object[] args);
        /// <summary>
        /// Warning
        /// </summary>
        void Warning(string line, params object[] args);
        /// <summary>
        /// Error
        /// </summary>
        void Error(string line, params object[] args);
        /// <summary>
        /// Error
        /// </summary>
        void Error(Exception ex, string line, params object[] args);
        /// <summary>
        /// Fatal crash
        /// </summary>
        void Fatal(string line, params object[] args);
        /// <summary>
        /// Fatal crash
        /// </summary>
        void Fatal(Exception ex, string line, params object[] args);
        /// <summary>
        /// Debug info
        /// </summary>
        void Debug(string line, params object[] args);
        /// <summary>
        /// Low Level Debug info
        /// </summary>
        void Verbose(string line, params object[] args);
    }
}
