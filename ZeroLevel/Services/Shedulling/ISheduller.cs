using System;

namespace ZeroLevel.Services.Shedulling
{
    public interface ISheduller : IDisposable
    {
        #region One time events

        long RemindAfter(TimeSpan timespan, Action<long> callback);

        long RemindAt(DateTime date, Action<long> callback);

        #endregion One time events

        #region Repitable behaviour

        /// <summary>
        /// Performs an action once a period, while the period is recalculated according to the transferred function at each re-creation of the task.
        /// </summary>
        /// <param name="nextEventPeriodCalcFunction">Function to calculate the next period</param>
        /// <param name="callback">Action</param>
        /// <returns>Task ID</returns>
        long RemindEveryNonlinearPeriod(Func<TimeSpan> nextEventPeriodCalcFunction, Action<long> callback,
            bool breakWherError = false);

        /// <summary>
        ///  Performs an action once a period, while the period is recalculated according to the transferred function at each re-creation of the task.
        /// </summary>
        /// <param name="firstEventPeriodCalcFunction">The function to calculate the period to the first execution</param>
        /// <param name="nextEventPeriodCalcFunction">The function for calculating the period until subsequent performances</param>
        /// <param name="callback">Action</param>
        /// <returns>Task ID</returns>
        long RemindEveryNonlinearPeriod(Func<TimeSpan> firstEventPeriodCalcFunction,
            Func<TimeSpan> nextEventPeriodCalcFunction, Action<long> callback,
            bool breakWherError = false);

        /// <summary>
        /// Performs an action once per period, while the date is recalculated from the function transferred each time the task is recreated.
        /// </summary>
        /// <param name="nextEventDateCalcFunction">The function to calculate the next date</param>
        /// <param name="callback">Action</param>
        /// <returns>Task ID</returns>
        long RemindEveryNonlinearDate(Func<DateTime, DateTime> nextEventDateCalcFunction, Action<long> callback,
            bool breakWherError = false);

        long RemindEveryNonlinearDate(DateTime firstTime, Func<DateTime, DateTime> nextEventDateCalcFunction,
            Action<long> callback,
            bool breakWherError = false);

        /// <summary>
        /// Performs an action once per period, while the date is recalculated from the function transferred each time the task is recreated.
        /// </summary>
        /// <param name="firstEventDateCalcFunction">The function to calculate the first run date</param>
        /// <param name="nextEventDateCalcFunction">The function to calculate the next date</param>
        /// <param name="callback">Action</param>
        /// <returns>Task ID</returns>
        long RemindEveryNonlinearDate(Func<DateTime, DateTime> firstEventDateCalcFunction,
            Func<DateTime, DateTime> nextEventDateCalcFunction, Action<long> callback,
            bool breakWherError = false);

        /// <summary>
        /// Performs an action once in a specified period
        /// </summary>
        /// <param name="timespan">Period</param>
        /// <param name="callback">Action</param>
        /// <returns>Task ID</returns>
        long RemindEvery(TimeSpan timespan, Action<long> callback,
            bool breakWherError = false);

        /// <summary>
        /// Performs an action once in a specified period
        /// </summary>
        /// <param name="first">Period to first run</param>
        /// <param name="next">Period</param>
        /// <param name="callback">Action</param>
        /// <returns>Task ID</returns>
        long RemindEvery(TimeSpan first, TimeSpan next, Action<long> callback,
            bool breakWherError = false);

        long RemindWhile(TimeSpan period, Func<long, bool> callback, Action continueWith = null,
            bool breakWherError = false);

        #endregion Repitable behaviour

        #region Sheduller control

        void Pause();

        void Resume();

        void Clean();

        bool Remove(long key);

        void SetInitialIndex(long index);

        #endregion Sheduller control
    }
}