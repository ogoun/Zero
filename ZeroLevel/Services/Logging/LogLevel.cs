using System;

namespace ZeroLevel.Services.Logging
{
    /// <summary>
    /// Перечисление, содержит возможные типы сообщений, для записи в лог
    /// </summary>
    [Flags]
    public enum LogLevel : int
    {
        None = 0,
        /// <summary>
        /// Сообщение
        /// </summary>
        Info = 1 << 0,
        /// <summary>
        /// Предупреждение о возможной неполадке
        /// </summary>
        Warning = 1 << 1,
        /// <summary>
        /// Ошибка в выполнении (некритичная)
        /// </summary>
        Error = 1 << 2,
        /// <summary>
        /// Ошибка приводящая к аварийному завершению программы
        /// </summary>
        Fatal = 1 << 3,
        /// <summary>
        /// Отладочная информация
        /// </summary>
        Debug = 1 << 4,
        /// <summary>
        /// Низкоуровневое логирование
        /// </summary>
        Verbose = 1 << 5,
        /// <summary>
        /// Стандартный уровень логирования, сообщения, предупреждения, ошибки и падения
        /// </summary>
        Standart = Info | Warning | Error | Fatal,
        /// <summary>
        /// Вывод сообщения как есть, без даты и уровня логирования
        /// </summary>
        Raw = 1 << 6,
        /// <summary>
        /// Запиcь проблем, предупреждения, ошибки, сбои
        /// </summary>
        Problem = Error | Fatal | Warning,
        /// <summary>
        /// Запись всех стандартных уровней, не включая отладочные
        /// </summary>
        All = Info | Problem | Raw,
        /// <summary>
        /// Все сообщения, включая отладочные и низкоуровневые
        /// </summary>
        FullDebug = All | Verbose | Debug,

        SystemInfo = 1 << 6,
        SystemWarning = 1 << 7,
        SystemError = 1 << 8,
        SystemFatal = 1 << 9,

        System = SystemInfo | SystemError | SystemWarning | SystemFatal,

        FullStandart = Standart | System
    }
}
