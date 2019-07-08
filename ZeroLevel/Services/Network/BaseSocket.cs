using System;

namespace ZeroLevel.Network
{
    public abstract class BaseSocket
    {
        static BaseSocket()
        {
            MAX_FRAME_PAYLOAD_SIZE = Configuration.Default.FirstOrDefault<int>("MAX_FRAME_PAYLOAD_SIZE", DEFAULT_MAX_FRAME_PAYLOAD_SIZE);
        }

        public static readonly IRouter NullRouter = new NullRouter();

        public const string DEFAULT_MESSAGE_INBOX = "__message_inbox__";
        public const string DEFAULT_REQUEST_INBOX = "__request_inbox__";
        public const string DEFAULT_REQUEST_WITHOUT_ARGS_INBOX = "__request_no_args_inbox__";
        protected const string DEFAULT_REQUEST_ERROR_INBOX = "__request_error__";

        /// <summary>
        /// Buffer size for receiving data
        /// </summary>
        protected const int DEFAULT_RECEIVE_BUFFER_SIZE = 4096;

        /// <summary>
        /// If during the specified period there was no network activity, send a ping-request
        /// </summary>
        protected const long HEARTBEAT_PING_PERIOD_TICKS = 1500 * TimeSpan.TicksPerMillisecond;

        /// <summary>
        /// Connection check period
        /// </summary>
        protected const int MINIMUM_HEARTBEAT_UPDATE_PERIOD_MS = 7500;

        /// <summary>
        /// The period of the request, after which it is considered unsuccessful
        /// </summary>
        internal const long MAX_REQUEST_TIME_TICKS = 30000 * TimeSpan.TicksPerMillisecond;

        public const int MAX_REQUEST_TIME_MS = 30000;

        /// <summary>
        /// Maximum size of data packet to transmit (serialized frame size)
        /// </summary>
        private const int DEFAULT_MAX_FRAME_PAYLOAD_SIZE = 1024 * 1024 * 32;
        public readonly static int MAX_FRAME_PAYLOAD_SIZE;

        /// <summary>
        /// The size of the message queue to send
        /// </summary>
        public const int MAX_SEND_QUEUE_SIZE = 1024;

        protected void Broken() => Status = Status == SocketClientStatus.Disposed ? Status : SocketClientStatus.Broken;
        protected void Disposed() => Status = SocketClientStatus.Disposed;
        protected void Working() => Status = Status == SocketClientStatus.Disposed ? Status : SocketClientStatus.Working;
        public SocketClientStatus Status { get; private set; } = SocketClientStatus.Initialized;

        public abstract void Dispose();
    }
}
