using System;
using ZeroLevel.Services.Logging;
using ZeroLevel.Services.Logging.Implementation;

namespace ZeroLevel
{
    public static class Log
    {
        #region Fields
        internal static ILogger _router;
        #endregion

        #region Ctor
        static Log()
        {
            _router = new LogRouter();
        }
        #endregion

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
        /// Сообщение
        /// </summary>
        public static void Info(string line, params object[] args)
        {
            _router.Write(LogLevel.Info, FormatMessage(line, args));
        }
        /// <summary>
        /// Предупреждение
        /// </summary>
        public static void Warning(string line, params object[] args)
        {
            _router.Write(LogLevel.Warning, FormatMessage(line, args));
        }
        /// <summary>
        /// Ошибка
        /// </summary>
        public static void Error(string line, params object[] args)
        {
            _router.Write(LogLevel.Error, FormatMessage(line, args));
        }
        /// <summary>
        /// Ошибка
        /// </summary>
        public static void Error(Exception ex, string line, params object[] args)
        {
            _router.Write(LogLevel.Error, FormatMessage(line, args) + "\r\n" + ex.ToString());
        }
        /// <summary>
        /// Фатальный сбой
        /// </summary>
        public static void Fatal(string line, params object[] args)
        {
            _router.Write(LogLevel.Fatal, FormatMessage(line, args));
        }
        /// <summary>
        /// Фатальный сбой
        /// </summary>
        public static void Fatal(Exception ex, string line, params object[] args)
        {
            _router.Write(LogLevel.Fatal, FormatMessage(line, args) + "\r\n" + ex.ToString());
        }
        /// <summary>
        /// Отладочная информация
        /// </summary>
        public static void Debug(string line, params object[] args)
        {
            _router.Write(LogLevel.Debug, FormatMessage(line, args));
        }
        /// <summary>
        /// Низкоуровневая отладолчная информация
        /// </summary>
        public static void Verbose(string line, params object[] args)
        {
            _router.Write(LogLevel.Verbose, FormatMessage(line, args));
        }


        /// <summary>
        /// Сообщение
        /// </summary>
        public static void SystemInfo(string line, params object[] args)
        {
            _router.Write(LogLevel.SystemInfo, FormatMessage(line, args));
        }
        /// <summary>
        /// Предупреждение
        /// </summary>
        public static void SystemWarning(string line, params object[] args)
        {
            _router.Write(LogLevel.SystemWarning, FormatMessage(line, args));
        }
        /// <summary>
        /// Ошибка
        /// </summary>
        public static void SystemError(string line, params object[] args)
        {
            _router.Write(LogLevel.SystemError, FormatMessage(line, args));
        }
        /// <summary>
        /// Ошибка
        /// </summary>
        public static void SystemError(Exception ex, string line, params object[] args)
        {
            _router.Write(LogLevel.SystemError, FormatMessage(line, args) + "\r\n" + ex.ToString());
        }
        /// <summary>
        /// Фатальный сбой
        /// </summary>
        public static void SystemFatal(string line, params object[] args)
        {
            _router.Write(LogLevel.SystemFatal, FormatMessage(line, args));
        }
        /// <summary>
        /// Фатальный сбой
        /// </summary>
        public static void SystemFatal(Exception ex, string line, params object[] args)
        {
            _router.Write(LogLevel.SystemFatal, FormatMessage(line, args) + "\r\n" + ex.ToString());
        }
        #endregion

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
        #endregion

        #region Console
        public static void AddConsoleLogger(LogLevel level = LogLevel.Standart)
        {
            AddLogger(new ConsoleLogger(), level);
        }
        #endregion

        #region TextLogs
        public static void AddTextFileLogger(TextFileLoggerOptions options, LogLevel level = LogLevel.FullDebug)
        {
            AddLogger(new TextFileLogger(options), level);
        }

        public static void AddTextFileLogger(string path, LogLevel level = LogLevel.FullDebug)
        {
            AddLogger(new FileLogger(path), level);
        }
        #endregion

        #region Encrypted file log
        public static void AddEncryptedFileLogger(EncryptedFileLogOptions options, LogLevel level = LogLevel.FullDebug)
        {
            AddLogger(new EncryptedFileLog(options), level);
        }
        #endregion

        #endregion

        #region Settings
        public static void CreateLoggingFromConfiguration()
        {
            CreateLoggingFromConfiguration(Configuration.ReadFromApplicationConfig());
        }

        public static void CreateLoggingFromConfiguration(IConfiguration config)
        {
            if (config.FirstOrDefault("console", false))
            {
                AddConsoleLogger();
            }
            if (config.Contains("log"))
            {
                var options = TextFileLoggerOptions.CreateOptionsBy(config, "log");
                if (options != null)
                {
                    AddTextFileLogger(options);
                }
            }
            var debug = string.Empty;
            if (config.Contains("debug"))
            {
                debug = "debug";
            }
            if (config.Contains("trace"))
            {
                debug = "trace";
            }
            if (false == string.IsNullOrWhiteSpace(debug))
            {
                var options = TextFileLoggerOptions.CreateOptionsBy(config, debug);
                if (options != null)
                {
                    AddTextFileLogger(options, LogLevel.Debug);
                }
            }
        }
        /// <summary>
        /// Установка максимального количества сообщений в очереди
        /// </summary>
        public static void Backlog(long backlog)
        {
            (_router as ILogComposer)?.SetupBacklog(backlog);
        }
        #endregion

        #region Disposable
        public static void Dispose()
        {
            _router.Dispose();
        }
        #endregion
    }
}
