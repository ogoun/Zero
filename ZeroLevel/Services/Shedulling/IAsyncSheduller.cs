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
        #endregion

        #region Repitable behaviour
        /// <summary>
        /// Исполняет действие раз в период, при этом период перерасчитывается по переданной функции при каждом пересоздании задачи
        /// </summary>
        /// <param name="nextEventPeriodCalcFunction">Функция для расчета следующего периода</param>
        /// <param name="callback">Действие</param>
        /// <returns>Идентификатор задания</returns>
        long RemindAsyncEveryNonlinearPeriod(Func<TimeSpan> nextEventPeriodCalcFunction, Func<long, Task> callback,
            bool breakWherError = false);
        /// <summary>
        ///  Исполняет действие раз в период, при этом период перерасчитывается по переданной функции при каждом пересоздании задачи
        /// </summary>
        /// <param name="firstEventPeriodCalcFunction">Функция для расчета периода до первого исполнения</param>
        /// <param name="nextEventPeriodCalcFunction">Функция для расчета периода до последующих исполнений</param>
        /// <param name="callback">Действие</param>
        /// <returns>Идентификатор задания</returns>
        long RemindAsyncEveryNonlinearPeriod(Func<TimeSpan> firstEventPeriodCalcFunction,
            Func<TimeSpan> nextEventPeriodCalcFunction, Func<long, Task> callback,
            bool breakWherError = false);
        /// <summary>
        /// Исполняет действие раз в период, при этом дата перерасчитывается по переданной функции при каждом пересоздании задачи
        /// </summary>
        /// <param name="nextEventDateCalcFunction">Функция для расчета следующей даты</param>
        /// <param name="callback">Действие</param>
        /// <returns>Идентификатор задания</returns>
        long RemindAsyncEveryNonlinearDate(Func<DateTime, DateTime> nextEventDateCalcFunction, Func<long, Task> callback,
            bool breakWherError = false);

        long RemindAsyncEveryNonlinearDate(DateTime firstTime, Func<DateTime, DateTime> nextEventDateCalcFunction,
            Func<long, Task> callback,
            bool breakWherError = false);
        /// <summary>
        /// Исполняет действие раз в период, при этом дата перерасчитывается по переданной функции при каждом пересоздании задачи
        /// </summary>
        /// <param name="firstEventDateCalcFunction">Функция для расчет даты первого запуска</param>
        /// <param name="nextEventDateCalcFunction">Функция для расчета следующей даты</param>
        /// <param name="callback">Действие</param>
        /// <returns>Идентификатор задания</returns>
        long RemindAsyncEveryNonlinearDate(Func<DateTime, DateTime> firstEventDateCalcFunction,
            Func<DateTime, DateTime> nextEventDateCalcFunction, Func<long, Task> callback,
            bool breakWherError = false);
        /// <summary>
        /// Исполняет действие раз в указанный период
        /// </summary>
        /// <param name="timespan">Период</param>
        /// <param name="callback">Действие</param>
        /// <returns>Идентификатор задания</returns>
        long RemindAsyncEvery(TimeSpan timespan, Func<long, Task> callback,
            bool breakWherError = false);
        /// <summary>
        /// Исполняет действие раз в указанный период
        /// </summary>
        /// <param name="first">Период до первого выполнения</param>
        /// <param name="next">Период</param>
        /// <param name="callback">Действие</param>
        /// <returns>Идентификатор задания</returns>
        long RemindAsyncEvery(TimeSpan first, TimeSpan next, Func<long, Task> callback,
            bool breakWherError = false);

        long RemindAsyncWhile(TimeSpan period, Func<long, Task<bool>> callback, Action continueWith = null,
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
