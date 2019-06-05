using System;
using ZeroLevel.Services.Logging;
using ZeroLevel.Services.Logging.Implementation;

namespace ZeroLevel
{
    public static class Log
    {
        #region Fields

        internal static ILogger _router;

        #endregion Fields

        #region Ctor

        static Log()
        {
            _router = new LogRouter();
        }

        #endregion Ctor

        private static string FormatMessage(string line, params object[] args)
        {
            if (args == null || args.Length == 0) return line;
            try
            {
                return string.Format(line, args);
            }
            catch
            {
                return line;
            }
        }

        #region Logging

        public static void Raw(string line, params object[] args)
        {
            _router.Write(LogLevel.Raw, FormatMessage(line, args));
        }

        /// <summary>
        /// Info message
        /// </summary>
        public static void Info(string line, params object[] args)
        {
            _router.Write(LogLevel.Info, FormatMessage(line, args));
        }

        /// <summary>
        /// Warning message
        /// </summary>
        public static void Warning(string line, params object[] args)
        {
            _router.Write(LogLevel.Warning, FormatMessage(line, args));
        }

        /// <summary>
        /// Error message
        /// </summary>
        public static void Error(string line, params object[] args)
        {
            _router.Write(LogLevel.Error, FormatMessage(line, args));
        }

        /// <summary>
        /// Error message
        /// </summary>
        public static void Error(Exception ex, string line, params object[] args)
        {
            _router.Write(LogLevel.Error, FormatMessage(line, args) + "\r\n" + ex.ToString());
        }

        /// <summary>
        /// Fatal crash
        /// </summary>
        public static void Fatal(string line, params object[] args)
        {
            _router.Write(LogLevel.Fatal, FormatMessage(line, args));
        }

        /// <summary>
        /// Fatal message (mean stop app after crash)
        /// </summary>
        public static void Fatal(Exception ex, string line, params object[] args)
        {
            _router.Write(LogLevel.Fatal, FormatMessage(line, args) + "\r\n" + ex.ToString());
        }

        /// <summary>
        /// Debug message
        /// </summary>
        public static void Debug(string line, params object[] args)
        {
            _router.Write(LogLevel.Debug, FormatMessage(line, args));
        }

        /// <summary>
        /// Low-level debug message
        /// </summary>
        public static void Verbose(string line, params object[] args)
        {
            _router.Write(LogLevel.Verbose, FormatMessage(line, args));
        }

        /// <summary>
        /// System message
        /// </summary>
        public static void SystemInfo(string line, params object[] args)
        {
            _router.Write(LogLevel.SystemInfo, FormatMessage(line, args));
        }

        /// <summary>
        /// System warning
        /// </summary>
        public static void SystemWarning(string line, params object[] args)
        {
            _router.Write(LogLevel.SystemWarning, FormatMessage(line, args));
        }

        /// <summary>
        /// System error
        /// </summary>
        public static void SystemError(string line, params object[] args)
        {
            _router.Write(LogLevel.SystemError, FormatMessage(line, args));
        }

        /// <summary>
        /// System error
        /// </summary>
        public static void SystemError(Exception ex, string line, params object[] args)
        {
            _router.Write(LogLevel.SystemError, FormatMessage(line, args) + "\r\n" + ex.ToString());
        }

        #endregion Logging

        #region Register loggers

        public static void AddLogger(ILogger logger, LogLevel level = LogLevel.Standart)
        {
            (_router as ILogComposer)?.AddLogger(logger, level);
        }

        #region Delegate logger

        public static void AddDelegateLogger(Action<string> handler, LogLevel level = LogLevel.Standart)
        {
            AddLogger(new DelegateLogger(handler), level);
        }

        #endregion Delegate logger

        #region Console

        public static void AddConsoleLogger(LogLevel level = LogLevel.Standart)
        {
            AddLogger(new ConsoleLogger(), level);
        }

        #endregion Console

        #region TextLogs

        public static void AddTextFileLogger(TextFileLoggerOptions options, LogLevel level = LogLevel.FullDebug)
        {
            AddLogger(new TextFileLogger(options), level);
        }

        public static void AddTextFileLogger(string path, LogLevel level = LogLevel.FullDebug)
        {
            AddLogger(new FileLogger(path), level);
        }

        #endregion TextLogs

        #region Encrypted file log

        public static void AddEncryptedFileLogger(EncryptedFileLogOptions options, LogLevel level = LogLevel.FullDebug)
        {
            AddLogger(new EncryptedFileLog(options), level);
        }

        #endregion Encrypted file log

        #endregion Register loggers

        #region Settings
        public static void CreateLoggingFromConfiguration(IConfigurationSet configSet)
        {
            IConfiguration config;
            bool log_section = false;
            if (configSet.Default.Contains("log"))
            {
                config = configSet.Default;
            }
            else if (configSet.ContainsSection("log"))
            {
                config = configSet["log"];
                log_section = true;
            }
            else
            {
                return;
            }
            string logPath = null;
            if (config.Contains("log"))
            {
                logPath = config.First("log");
            }
            else if (log_section && config.Contains("path"))
            {
                logPath = config.First("path");
            }
            if (false == string.IsNullOrWhiteSpace(logPath))
            {
                var options = TextFileLoggerOptions.CreateOptionsBy(config, logPath, log_section ? string.Empty : "log.");
                if (options != null)
                {
                    AddTextFileLogger(options);
                }
            }
            if (config.FirstOrDefault("console", false))
            {
                AddConsoleLogger(LogLevel.System | LogLevel.FullDebug);
            }
        }

        /// <summary>
        /// Set max count log-messages in queue
        /// </summary>
        public static void Backlog(long backlog)
        {
            (_router as ILogComposer)?.SetupBacklog(backlog);
        }

        #endregion Settings

        #region Disposable

        public static void Dispose()
        {
            _router.Dispose();
        }

        #endregion Disposable
    }
}