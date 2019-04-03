using System;

namespace ZeroLevel.Services.Network
{
    public abstract class ZBaseNetwork
        : IDisposable
    {
        public const string DEFAULT_MESSAGE_INBOX = "__message_inbox__";
        public const string DEFAULT_REQUEST_INBOX = "__request_inbox__";

        protected const string DEFAULT_PING_INBOX = "__ping__";
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
        protected const int HEARTBEAT_UPDATE_PERIOD_MS = 7500;
        /// <summary>
        /// The period of the request, after which it is considered unsuccessful
        /// </summary>
        protected const long MAX_REQUEST_TIME_TICKS = 30000 * TimeSpan.TicksPerMillisecond;
        public const int MAX_REQUEST_TIME_MS = 30000;
        /// <summary>
        /// Maximum size of data packet to transmit (serialized frame size)
        /// </summary>
        public const int MAX_FRAME_PAYLOAD_SIZE = 1024 * 1024 * 32;
        /// <summary>
        /// Starting byte of the data packet header
        /// </summary>
        public const byte PACKET_HEADER_START_BYTE = 181;
        /// <summary>
        /// The size of the message queue to send
        /// </summary>
        public const int MAX_SEND_QUEUE_SIZE = 1024;

        protected ZTransportStatus _status = ZTransportStatus.Initialized;
        public ZTransportStatus Status { get { return _status; } }

        public abstract void Dispose();
    }
}
