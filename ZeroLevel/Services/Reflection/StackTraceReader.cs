using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ZeroLevel.Services.Reflection
{
    /// <summary>
    /// Performs read of stack trace
    /// </summary>
    public static class StackTraceReader
    {
        /// <summary>
        /// Read current stack trace
        /// </summary>
        /// <returns>Result - enumerable of tuples as 'Type':'MethodName'</returns>
        public static Tuple<string, string>[] ReadFrames()
        {
            var result = new List<Tuple<string, string>>();
            var stackTrace = new StackTrace();
            foreach (var frame in stackTrace.GetFrames() ?? Enumerable.Empty<StackFrame>())
            {
                var method = frame.GetMethod();
                if (method != null && method.DeclaringType != null)
                {
                    var type = method.DeclaringType.Name;
                    if (false == type.Equals("StackTraceReader", StringComparison.Ordinal))
                    {
                        result.Add(new Tuple<string, string>(type, method.Name));
                    }
                }
            }
            return result.ToArray();
        }
    }
}
