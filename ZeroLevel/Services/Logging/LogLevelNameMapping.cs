using System.Collections.Generic;
using ZeroLevel.Logging;

namespace ZeroLevel.Logging
{
    internal static class LogLevelNameMapping
    {
        private static readonly Dictionary<LogLevel, string> _compact = new Dictionary<LogLevel, string>
        {
            { LogLevel.Error, "ERR"},
            { LogLevel.Fatal, "FLT"},
            { LogLevel.Info, "INF"},
            { LogLevel.None, ""},
            { LogLevel.Verbose, "VRB"},
            { LogLevel.Warning, "WRN"},
            { LogLevel.Debug, "DBG"},

            { LogLevel.SystemInfo, "SYSINF"},
            { LogLevel.SystemError, "SYSERR"},
            { LogLevel.SystemWarning, "SYSWRN"}
        };

        private static readonly Dictionary<LogLevel, string> _full = new Dictionary<LogLevel, string>
        {
            { LogLevel.Error, " error"},
            { LogLevel.Fatal, " fatal"},
            { LogLevel.Info, " info"},
            { LogLevel.None, ""},
            { LogLevel.Verbose, " verbose"},
            { LogLevel.Warning, " warning"},
            { LogLevel.Debug, " debug"},

            { LogLevel.SystemInfo, " system info"},
            { LogLevel.SystemError, " system error"},
            { LogLevel.SystemWarning, " systm warning"}
        };

        public static string CompactName(LogLevel level)
        {
            return _compact[level];
        }

        public static string FullName(LogLevel level)
        {
            return _full[level];
        }
    }
}