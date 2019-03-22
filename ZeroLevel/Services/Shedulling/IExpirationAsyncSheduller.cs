using System;
using System.Threading.Tasks;

namespace ZeroLevel.Services.Shedulling
{
    public interface IExpirationAsyncSheduller : IDisposable
    {
        /// <summary>
        /// Добавление задачи с указанием времени по истечении которого она должна быть исполнена
        /// </summary>
        long Push(TimeSpan timespan, Func<long, Task> callback);
        /// <summary>
        /// Добавление задачи с указанием даты/времени когда она должна быть исполнена
        /// </summary>
        long Push(DateTime date, Func<long, Task> callback);
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
