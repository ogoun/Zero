using System;

namespace ZeroLevel.Services.Applications
{
    [Flags]
    public enum ZeroServiceState : int
    {
        Initialized = 0,
        /// <summary>
        /// Сервис работает
        /// </summary>
        Started = 1,
        /// <summary>
        /// Работа сервиса приостановлена
        /// </summary>
        Paused = 2,
        /// <summary>
        /// Сервис остановлен (ресурсы освобождены)
        /// </summary>
        Stopped = 3
    }
}
