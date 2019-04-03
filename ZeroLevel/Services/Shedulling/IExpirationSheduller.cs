using System;

namespace ZeroLevel.Services.Shedulling
{
    public interface IExpirationSheduller
        : IDisposable
    {
        /// <summary>
        /// Adding a task with the time after which it should be completed
        /// </summary>
        long Push(TimeSpan timespan, Action<long> callback);

        /// <summary>
        /// Adding a task with the date / time when it should be executed
        /// </summary>
        long Push(DateTime date, Action<long> callback);

        /// <summary>
        /// Delete task by ID
        /// </summary>
        /// <param name="key">Task ID</param>
        bool Remove(long key);

        /// <summary>
        /// Cleaning the scheduler
        /// </summary>
        void Clean();

        /// <summary>
        /// Pausing the scheduler (does not prevent the addition of new tasks)
        /// </summary>
        void Pause();

        /// <summary>
        /// Resumption of work scheduler
        /// </summary>
        void Resume();
    }
}