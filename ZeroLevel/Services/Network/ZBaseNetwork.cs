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
        /// Размер буфера для приема данных
        /// </summary>
        protected const int DEFAULT_RECEIVE_BUFFER_SIZE = 4096;
        /// <summary>
        /// Если в течение указанного периода не было сетевой активности, выслать пинг-реквест
        /// </summary>
        protected const long HEARTBEAT_PING_PERIOD_TICKS = 1500 * TimeSpan.TicksPerMillisecond;
        /// <summary>
        /// Период проверки наличия соединения
        /// </summary>
        protected const int HEARTBEAT_UPDATE_PERIOD_MS = 7500;
        /// <summary>
        /// Период выполнения запроса, после которого считать его неудачным
        /// </summary>
        protected const long MAX_REQUEST_TIME_TICKS = 30000 * TimeSpan.TicksPerMillisecond;
        public const int MAX_REQUEST_TIME_MS = 30000;
        /// <summary>
        /// Максимальный размер пакета данных для передачи (сериализованный размер фрейма)
        /// </summary>
        public const int MAX_FRAME_PAYLOAD_SIZE = 1024 * 1024 * 32;
        /// <summary>
        /// Начальный байт заголовка пакета данных
        /// </summary>
        public const byte PACKET_HEADER_START_BYTE = 181;
        /// <summary>
        /// Размер очереди сообщения для отправки
        /// </summary>
        public const int MAX_SEND_QUEUE_SIZE = 1024;

        protected ZTransportStatus _status = ZTransportStatus.Initialized;
        public ZTransportStatus Status { get { return _status; } }

        public abstract void Dispose();
    }
}
