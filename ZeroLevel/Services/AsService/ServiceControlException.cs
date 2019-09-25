using System;
using System.Runtime.Serialization;

namespace ZeroLevel.Services.AsService
{
    [Serializable]
    public class ServiceControlException :
        Exception
    {
        public ServiceControlException()
        {
        }

        public ServiceControlException(string message)
            : base(message)
        {
        }

        public ServiceControlException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected ServiceControlException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public ServiceControlException(string format, Type serviceType, string command, Exception innerException)
            : this(FormatMessage(format, serviceType, command), innerException)
        {

        }

        static string FormatMessage(string format, Type serviceType, string command)
        {
            return string.Format(format, serviceType, command);
        }
    }
}
