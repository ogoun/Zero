using System;
using System.Threading.Tasks;

namespace ZeroLevel.Services.Shedulling
{
    public interface IAsyncSheduller
        : IDisposable
    {
        #region One time events

        long RemindAsyncAfter(TimeSpan timespan, Func<long, Task> callback);

        long RemindAsyncAt(DateTime date, Func<long, Task> callback);

        #endregion One time events

        #region Repitable behaviour

        /// <summary>
        /// Performs an action once a period, while the period is recalculated according to the transferred function at each re-creation of the task.
        /// </summary>
        /// <param name="nextEventPeriodCalcFunction">Function to calculate the next period</param>
        /// <param name="callback">Action</param>
        /// <returns>Task ID</returns>
        long RemindAsyncEveryNonlinearPeriod(Func<TimeSpan> nextEventPeriodCalcFunction, Func<long, Task> callback,
            bool breakWherError = false);

        /// <summary>
        ///  Performs an action once a period, while the period is recalculated according to the transferred function at each re-creation of the task.
        /// </summary>
        /// <param name="firstEventPeriodCalcFunction">The function to calculate the period to the first execution</param>
        /// <param name="nextEventPeriodCalcFunction">The function for calculating the period until subsequent performances</param>
        /// <param name="callback">Action</param>
        /// <returns>Task ID</returns>
        long RemindAsyncEveryNonlinearPeriod(Func<TimeSpan> firstEventPeriodCalcFunction,
            Func<TimeSpan> nextEventPeriodCalcFunction, Func<long, Task> callback,
            bool breakWherError = false);

        /// <summary>
        /// Performs an action once per period, while the date is recalculated from the function transferred each time the task is recreated.
        /// </summary>
        /// <param name="nextEventDateCalcFunction">The function to calculate the next date</param>
        /// <param name="callback">Action</param>
        /// <returns>Task ID</returns>
        long RemindAsyncEveryNonlinearDate(Func<DateTime, DateTime> nextEventDateCalcFunction, Func<long, Task> callback,
            bool breakWherError = false);

        long RemindAsyncEveryNonlinearDate(DateTime firstTime, Func<DateTime, DateTime> nextEventDateCalcFunction,
            Func<long, Task> callback,
            bool breakWherError = false);

        /// <summary>
        /// Performs an action once per period, while the date is recalculated from the function transferred each time the task is recreated.
        /// </summary>
        /// <param name="firstEventDateCalcFunction">The function to calculate the first run date</param>
        /// <param name="nextEventDateCalcFunction">The function to calculate the next date</param>
        /// <param name="callback">Action</param>
        /// <returns>Task ID</returns>
        long RemindAsyncEveryNonlinearDate(Func<DateTime, DateTime> firstEventDateCalcFunction,
            Func<DateTime, DateTime> nextEventDateCalcFunction, Func<long, Task> callback,
            bool breakWherError = false);

        /// <summary>
        /// Performs an action once in a specified period
        /// </summary>
        /// <param name="timespan">Period</param>
        /// <param name="callback">Action</param>
        /// <returns>Task ID</returns>
        long RemindAsyncEvery(TimeSpan timespan, Func<long, Task> callback,
            bool breakWherError = false);

        /// <summary>
        /// Performs an action once in a specified period
        /// </summary>
        /// <param name="first">Period to first run</param>
        /// <param name="next">Period</param>
        /// <param name="callback">Action</param>
        /// <returns>Task ID</returns>
        long RemindAsyncEvery(TimeSpan first, TimeSpan next, Func<long, Task> callback,
            bool breakWherError = false);

        long RemindAsyncWhile(TimeSpan period, Func<long, Task<bool>> callback, Action continueWith = null,
            bool breakWherError = false);

        #endregion Repitable behaviour

        #region Sheduller control

        void Pause();

        void Resume();

        void Clean();

        bool Remove(long key);

        #endregion Sheduller control
    }
}