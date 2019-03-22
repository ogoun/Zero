using System;

namespace ZeroLevel.Services.Shedulling
{
    public interface ISheduller : IDisposable
    {
        #region One time events
        long RemindAfter(TimeSpan timespan, Action<long> callback);
        long RemindAt(DateTime date, Action<long> callback);
        #endregion

        #region Repitable behaviour
        /// <summary>
        /// Исполняет действие раз в период, при этом период перерасчитывается по переданной функции при каждом пересоздании задачи
        /// </summary>
        /// <param name="nextEventPeriodCalcFunction">Функция для расчета следующего периода</param>
        /// <param name="callback">Действие</param>
        /// <returns>Идентификатор задания</returns>
        long RemindEveryNonlinearPeriod(Func<TimeSpan> nextEventPeriodCalcFunction, Action<long> callback,
            bool breakWherError = false);
        /// <summary>
        ///  Исполняет действие раз в период, при этом период перерасчитывается по переданной функции при каждом пересоздании задачи
        /// </summary>
        /// <param name="firstEventPeriodCalcFunction">Функция для расчета периода до первого исполнения</param>
        /// <param name="nextEventPeriodCalcFunction">Функция для расчета периода до последующих исполнений</param>
        /// <param name="callback">Действие</param>
        /// <returns>Идентификатор задания</returns>
        long RemindEveryNonlinearPeriod(Func<TimeSpan> firstEventPeriodCalcFunction,
            Func<TimeSpan> nextEventPeriodCalcFunction, Action<long> callback,
            bool breakWherError = false);
        /// <summary>
        /// Исполняет действие раз в период, при этом дата перерасчитывается по переданной функции при каждом пересоздании задачи
        /// </summary>
        /// <param name="nextEventDateCalcFunction">Функция для расчета следующей даты</param>
        /// <param name="callback">Действие</param>
        /// <returns>Идентификатор задания</returns>
        long RemindEveryNonlinearDate(Func<DateTime, DateTime> nextEventDateCalcFunction, Action<long> callback,
            bool breakWherError = false);

        long RemindEveryNonlinearDate(DateTime firstTime, Func<DateTime, DateTime> nextEventDateCalcFunction,
            Action<long> callback,
            bool breakWherError = false);
        /// <summary>
        /// Исполняет действие раз в период, при этом дата перерасчитывается по переданной функции при каждом пересоздании задачи
        /// </summary>
        /// <param name="firstEventDateCalcFunction">Функция для расчет даты первого запуска</param>
        /// <param name="nextEventDateCalcFunction">Функция для расчета следующей даты</param>
        /// <param name="callback">Действие</param>
        /// <returns>Идентификатор задания</returns>
        long RemindEveryNonlinearDate(Func<DateTime, DateTime> firstEventDateCalcFunction,
            Func<DateTime, DateTime> nextEventDateCalcFunction, Action<long> callback,
            bool breakWherError = false);
        /// <summary>
        /// Исполняет действие раз в указанный период
        /// </summary>
        /// <param name="timespan">Период</param>
        /// <param name="callback">Действие</param>
        /// <returns>Идентификатор задания</returns>
        long RemindEvery(TimeSpan timespan, Action<long> callback,
            bool breakWherError = false);
        /// <summary>
        /// Исполняет действие раз в указанный период
        /// </summary>
        /// <param name="first">Период до первого выполнения</param>
        /// <param name="next">Период</param>
        /// <param name="callback">Действие</param>
        /// <returns>Идентификатор задания</returns>
        long RemindEvery(TimeSpan first, TimeSpan next, Action<long> callback,
            bool breakWherError = false);

        long RemindWhile(TimeSpan period, Func<long, bool> callback, Action continueWith = null,
            bool breakWherError = false);
        #endregion

        #region Sheduller control
        void Pause();
        void Resume();
        void Clean();
        bool Remove(long key);
        #endregion
    }
}
