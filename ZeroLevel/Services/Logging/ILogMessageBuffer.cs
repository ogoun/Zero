using System;

namespace ZeroLevel.Services.Logging
{
    /// <summary>
    /// Очередь сообщений для вывода в лог
    /// </summary>
    internal interface ILogMessageBuffer : IDisposable
    {
        /// <summary>
        /// Количество сообщений в очереди
        /// </summary>
        long Count { get; }
        /// <summary>
        /// Запись сообщения в очередь
        /// </summary>
        void Push(LogLevel level, string message);
        /// <summary>
        /// Запрос сообщения из очереди для вывода в лог, подразумевается блокирующая работа метода,
        /// пока очередь пустая, метод ожидает появления сообщения не возвращая результат.
        /// </summary>
        Tuple<LogLevel, string> Take();
    }
}
