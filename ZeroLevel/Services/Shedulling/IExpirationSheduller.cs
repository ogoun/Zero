using System;

namespace ZeroLevel.Services.Shedulling
{
    public interface IExpirationSheduller 
        : IDisposable
    {
        /// <summary>
        /// Добавление задачи с указанием времени по истечении которого она должна быть исполнена
        /// </summary>
        long Push(TimeSpan timespan, Action<long> callback);
        /// <summary>
        /// Добавление задачи с указанием даты/времени когда она должна быть исполнена
        /// </summary>
        long Push(DateTime date, Action<long> callback);
        /// <summary>
        /// Удаляет событие по его идентификатору
        /// </summary>
        /// <param name="key">Идентификатор события</param>
        bool Remove(long key);
        /// <summary>
        /// Очистка планировщика
        /// </summary>
        void Clean();
        /// <summary>
        /// Приостановка работы планировщика (не препятствует добавлению новых заданий)
        /// </summary>
        void Pause();
        /// <summary>
        /// Возобновление работы планировщика
        /// </summary>
        void Resume();
    }
}
