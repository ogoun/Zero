using System;

namespace ZeroLevel.Services.Logging
{
    public interface ILog
    {
        /// <summary>
        /// Вывод сообщения как есть, без добавления уровня логирования и даты
        /// </summary>
        void Raw(string line, params object[] args);
        /// <summary>
        /// Сообщение
        /// </summary>
        void Info(string line, params object[] args);
        /// <summary>
        /// Предупреждение
        /// </summary>
        void Warning(string line, params object[] args);
        /// <summary>
        /// Ошибка
        /// </summary>
        void Error(string line, params object[] args);
        /// <summary>
        /// Ошибка
        /// </summary>
        void Error(Exception ex, string line, params object[] args);
        /// <summary>
        /// Фатальный сбой
        /// </summary>
        void Fatal(string line, params object[] args);
        /// <summary>
        /// Фатальный сбой
        /// </summary>
        void Fatal(Exception ex, string line, params object[] args);
        /// <summary>
        /// Отладочная информация
        /// </summary>
        void Debug(string line, params object[] args);
        /// <summary>
        /// Низкоуровневая отладочная информация
        /// </summary>
        void Verbose(string line, params object[] args);
    }
}
