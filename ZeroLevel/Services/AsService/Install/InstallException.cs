using System;
using System.Runtime.Serialization;

namespace ZeroLevel.Services.AsService
{
    [Serializable]
    internal class InstallException : SystemException
    {
        public InstallException()
        {
            base.HResult = -2146232057;
        }

        public InstallException(string message) : base(message)
        {
        }

        public InstallException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InstallException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
