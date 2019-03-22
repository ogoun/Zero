using System;
using System.Threading.Tasks;
using ZeroLevel.Services.Shedulling;

namespace ZeroLevel
{
    /// <summary>
    /// Планировщик по умолчанию, предназначен для служебных задач
    /// </summary>
    public static class Sheduller
    {
        #region Factory
        public static IExpirationSheduller CreateExpirationSheduller()
        {
            return new DateTimeSheduller();
        }

        public static IExpirationAsyncSheduller CreateAsyncExpirationSheduller()
        {
            return new DateTimeAsyncSheduller();
        }

        public static ISheduller Create()
        {
            return new ShedullerImpl();
        }

        public static IAsyncSheduller CreateAsync()
        {
            return new AsyncShedullerImpl();
        }
        #endregion

        #region Singletones
        private static ISheduller __instanse;
        private static readonly object _create_lock = new object();
        private static ISheduller DefaultInstance
        {
            get
            {
                if (__instanse == null)
                {
                    lock (_create_lock)
                    {
                        if (__instanse == null)
                        {
                            __instanse = Sheduller.Create();
                        }
                    }
                }
                return __instanse;
            }
        }

        private static IAsyncSheduller __async_instance;
        private static readonly object _async_create_lock = new object();
        private static IAsyncSheduller DefaultAsyncInstance
        {
            get
            {
                if (__async_instance == null)
                {
                    lock (_create_lock)
                    {
                        if (__async_instance == null)
                        {
                            __async_instance = Sheduller.CreateAsync();
                        }
                    }
                }
                return __async_instance;
            }
        }
        #endregion

        #region Sync default instance api
        public static long RemindWhile(TimeSpan timespan, Func<long, bool> callback, Action continueWith = null)
        {
            return DefaultInstance.RemindWhile(timespan, callback, continueWith);
        }
        public static long RemindWhile(TimeSpan timespan, Func<bool> callback, Action continueWith = null)
        {
            return DefaultInstance.RemindWhile(timespan, (k) => callback(), continueWith);
        }

        public static long RemindAfter(TimeSpan timespan, Action<long> callback)
        {
            return DefaultInstance.RemindAfter(timespan, callback);
        }
        public static long RemindAfter(TimeSpan timespan, Action callback)
        {
            return DefaultInstance.RemindAfter(timespan, k => callback());
        }

        public static long RemindAt(DateTime date, Action<long> callback)
        {
            return DefaultInstance.RemindAt(date, callback);
        }
        public static long RemindAt(DateTime date, Action callback)
        {
            return DefaultInstance.RemindAt(date, k => callback());
        }

        public static long RemindEvery(TimeSpan first, TimeSpan next, Action<long> callback)
        {
            return DefaultInstance.RemindEvery(first, next, callback);
        }
        public static long RemindEvery(TimeSpan first, TimeSpan next, Action callback)
        {
            return DefaultInstance.RemindEvery(first, next, k => callback());
        }

        public static long RemindEvery(TimeSpan timespan, Action<long> callback)
        {
            return DefaultInstance.RemindEvery(timespan, callback);
        }
        public static long RemindEvery(TimeSpan timespan, Action callback)
        {
            return DefaultInstance.RemindEvery(timespan, k => callback());
        }

        /// <summary>
        /// Исполняет действие раз в период, при этом период перерасчитывается по переданной функции при каждом пересоздании задачи
        /// </summary>
        /// <param name="nextEventPeriodCalcFunction">Функция для расчета следующего периода</param>
        /// <param name="callback">Действие</param>
        /// <returns>Идентификатор задания</returns>
        public static long RemindEveryNonlinearPeriod(Func<TimeSpan> nextEventPeriodCalcFunction, Action<long> callback)
        {
            return DefaultInstance.RemindEveryNonlinearPeriod(nextEventPeriodCalcFunction, nextEventPeriodCalcFunction, callback);
        }
        public static long RemindEveryNonlinearPeriod(Func<TimeSpan> nextEventPeriodCalcFunction, Action callback)
        {
            return DefaultInstance.RemindEveryNonlinearPeriod(nextEventPeriodCalcFunction, nextEventPeriodCalcFunction, k => callback());
        }
        /// <summary>
        ///  Исполняет действие раз в период, при этом период перерасчитывается по переданной функции при каждом пересоздании задачи
        /// </summary>
        /// <param name="firstEventPeriodCalcFunction">Функция для расчета периода до первого исполнения</param>
        /// <param name="nextEventPeriodCalcFunction">Функция для расчета периода до последующих исполнений</param>
        /// <param name="callback">Действие</param>
        /// <returns>Идентификатор задания</returns>
        public static long RemindEveryNonlinearPeriod(Func<TimeSpan> firstEventPeriodCalcFunction, Func<TimeSpan> nextEventPeriodCalcFunction, Action<long> callback)
        {
            return DefaultInstance.RemindEveryNonlinearPeriod(firstEventPeriodCalcFunction, nextEventPeriodCalcFunction, callback);
        }
        public static long RemindEveryNonlinearPeriod(Func<TimeSpan> firstEventPeriodCalcFunction, Func<TimeSpan> nextEventPeriodCalcFunction, Action callback)
        {
            return DefaultInstance.RemindEveryNonlinearPeriod(firstEventPeriodCalcFunction, nextEventPeriodCalcFunction, k => callback());
        }
        /// <summary>
        /// Исполняет действие раз в период, при этом дата перерасчитывается по переданной функции при каждом пересоздании задачи
        /// </summary>
        /// <param name="nextEventDateCalcFunction">Функция для расчета следующей даты</param>
        /// <param name="callback">Действие</param>
        /// <returns>Идентификатор задания</returns>
        public static long RemindEveryNonlinearDate(Func<DateTime, DateTime> nextEventDateCalcFunction, Action<long> callback)
        {
            return DefaultInstance.RemindEveryNonlinearDate(nextEventDateCalcFunction, nextEventDateCalcFunction, callback);
        }
        public static long RemindEveryNonlinearDate(Func<DateTime, DateTime> nextEventDateCalcFunction, Action callback)
        {
            return DefaultInstance.RemindEveryNonlinearDate(nextEventDateCalcFunction, nextEventDateCalcFunction, k => callback());
        }
        /// <summary>
        /// Исполняет действие раз в период, при этом дата перерасчитывается по переданной функции при каждом пересоздании задачи
        /// </summary>
        /// <param name="firstEventDateCalcFunction">Функция для расчет даты первого запуска</param>
        /// <param name="nextEventDateCalcFunction">Функция для расчета следующей даты</param>
        /// <param name="callback">Действие</param>
        /// <returns>Идентификатор задания</returns>
        public static long RemindEveryNonlinearDate(Func<DateTime, DateTime> firstEventDateCalcFunction,
            Func<DateTime, DateTime> nextEventDateCalcFunction, Action<long> callback)
        {
            return DefaultInstance.RemindEveryNonlinearDate(firstEventDateCalcFunction, nextEventDateCalcFunction, callback);
        }
        public static long RemindEveryNonlinearDate(Func<DateTime, DateTime> firstEventDateCalcFunction,
            Func<DateTime, DateTime> nextEventDateCalcFunction, Action callback)
        {
            return DefaultInstance.RemindEveryNonlinearDate(firstEventDateCalcFunction, nextEventDateCalcFunction, k => callback());
        }

        public static bool Remove(long key)
        {
            return DefaultInstance.Remove(key);
        }
        #endregion

        #region Async default instance api
        public static long RemindAsyncWhile(TimeSpan timespan, Func<long, Task<bool>> callback, Action continueWith = null)
        {
            return DefaultAsyncInstance.RemindAsyncWhile(timespan, callback, continueWith);
        }
        public static long RemindAsyncWhile(TimeSpan timespan, Func<Task<bool>> callback, Action continueWith = null)
        {
            return DefaultAsyncInstance.RemindAsyncWhile(timespan, (k) => callback(), continueWith);
        }

        public static long RemindAsyncAfter(TimeSpan timespan, Func<long, Task> callback)
        {
            return DefaultAsyncInstance.RemindAsyncAfter(timespan, callback);
        }
        public static long RemindAsyncAfter(TimeSpan timespan, Func<Task> callback)
        {
            return DefaultAsyncInstance.RemindAsyncAfter(timespan, async k => await callback());
        }

        public static long RemindAsyncAt(DateTime date, Func<long, Task> callback)
        {
            return DefaultAsyncInstance.RemindAsyncAt(date, callback);
        }
        public static long RemindAsyncAt(DateTime date, Func<Task> callback)
        {
            return DefaultAsyncInstance.RemindAsyncAt(date, async k => await callback());
        }

        public static long RemindAsyncEvery(TimeSpan first, TimeSpan next, Func<long, Task> callback)
        {
            return DefaultAsyncInstance.RemindAsyncEvery(first, next, callback);
        }
        public static long RemindAsyncEvery(TimeSpan first, TimeSpan next, Func<Task> callback)
        {
            return DefaultAsyncInstance.RemindAsyncEvery(first, next, async k => await callback());
        }

        public static long RemindAsyncEvery(TimeSpan timespan, Func<long, Task> callback)
        {
            return DefaultAsyncInstance.RemindAsyncEvery(timespan, callback);
        }
        public static long RemindAsyncEvery(TimeSpan timespan, Func<Task> callback)
        {
            return DefaultAsyncInstance.RemindAsyncEvery(timespan, async k => await callback());
        }

        /// <summary>
        /// Исполняет действие раз в период, при этом период перерасчитывается по переданной функции при каждом пересоздании задачи
        /// </summary>
        /// <param name="nextEventPeriodCalcFunction">Функция для расчета следующего периода</param>
        /// <param name="callback">Действие</param>
        /// <returns>Идентификатор задания</returns>
        public static long RemindAsyncEveryNonlinearPeriod(Func<TimeSpan> nextEventPeriodCalcFunction, Func<long, Task> callback)
        {
            return DefaultAsyncInstance.RemindAsyncEveryNonlinearPeriod(nextEventPeriodCalcFunction, nextEventPeriodCalcFunction, callback);
        }
        public static long RemindAsyncEveryNonlinearPeriod(Func<TimeSpan> nextEventPeriodCalcFunction, Func<Task> callback)
        {
            return DefaultAsyncInstance.RemindAsyncEveryNonlinearPeriod(nextEventPeriodCalcFunction, nextEventPeriodCalcFunction, async k => await callback());
        }
        /// <summary>
        ///  Исполняет действие раз в период, при этом период перерасчитывается по переданной функции при каждом пересоздании задачи
        /// </summary>
        /// <param name="firstEventPeriodCalcFunction">Функция для расчета периода до первого исполнения</param>
        /// <param name="nextEventPeriodCalcFunction">Функция для расчета периода до последующих исполнений</param>
        /// <param name="callback">Действие</param>
        /// <returns>Идентификатор задания</returns>
        public static long RemindAsyncEveryNonlinearPeriod(Func<TimeSpan> firstEventPeriodCalcFunction, Func<TimeSpan> nextEventPeriodCalcFunction, Func<long, Task> callback)
        {
            return DefaultAsyncInstance.RemindAsyncEveryNonlinearPeriod(firstEventPeriodCalcFunction, nextEventPeriodCalcFunction, callback);
        }
        public static long RemindAsyncEveryNonlinearPeriod(Func<TimeSpan> firstEventPeriodCalcFunction, Func<TimeSpan> nextEventPeriodCalcFunction, Func<Task> callback)
        {
            return DefaultAsyncInstance.RemindAsyncEveryNonlinearPeriod(firstEventPeriodCalcFunction, nextEventPeriodCalcFunction, async k => await callback());
        }
        /// <summary>
        /// Исполняет действие раз в период, при этом дата перерасчитывается по переданной функции при каждом пересоздании задачи
        /// </summary>
        /// <param name="nextEventDateCalcFunction">Функция для расчета следующей даты</param>
        /// <param name="callback">Действие</param>
        /// <returns>Идентификатор задания</returns>
        public static long RemindAsyncEveryNonlinearDate(Func<DateTime, DateTime> nextEventDateCalcFunction, Func<long, Task> callback)
        {
            return DefaultAsyncInstance.RemindAsyncEveryNonlinearDate(nextEventDateCalcFunction, nextEventDateCalcFunction, callback);
        }
        public static long RemindAsyncEveryNonlinearDate(Func<DateTime, DateTime> nextEventDateCalcFunction, Func<Task> callback)
        {
            return DefaultAsyncInstance.RemindAsyncEveryNonlinearDate(nextEventDateCalcFunction, nextEventDateCalcFunction, async k => await callback());
        }
        /// <summary>
        /// Исполняет действие раз в период, при этом дата перерасчитывается по переданной функции при каждом пересоздании задачи
        /// </summary>
        /// <param name="firstEventDateCalcFunction">Функция для расчет даты первого запуска</param>
        /// <param name="nextEventDateCalcFunction">Функция для расчета следующей даты</param>
        /// <param name="callback">Действие</param>
        /// <returns>Идентификатор задания</returns>
        public static long RemindAsyncEveryNonlinearDate(Func<DateTime, DateTime> firstEventDateCalcFunction,
            Func<DateTime, DateTime> nextEventDateCalcFunction, Func<long, Task> callback)
        {
            return DefaultAsyncInstance.RemindAsyncEveryNonlinearDate(firstEventDateCalcFunction, nextEventDateCalcFunction, callback);
        }
        public static long RemindAsyncEveryNonlinearDate(Func<DateTime, DateTime> firstEventDateCalcFunction,
            Func<DateTime, DateTime> nextEventDateCalcFunction, Func<Task> callback)
        {
            return DefaultAsyncInstance.RemindAsyncEveryNonlinearDate(firstEventDateCalcFunction, nextEventDateCalcFunction, async k => await callback());
        }

        public static bool RemoveAsync(long key)
        {
            return DefaultAsyncInstance.Remove(key);
        }
        #endregion

        #region Default instances control
        public static void Pause()
        {
            DefaultInstance.Pause();
            DefaultAsyncInstance.Pause();
        }

        public static void Resume()
        {
            DefaultInstance.Resume();
            DefaultAsyncInstance.Resume();
        }

        public static void Clean()
        {
            DefaultInstance.Clean();
            DefaultAsyncInstance.Clean();
        }

        public static void Dispose()
        {
            DefaultAsyncInstance.Dispose();
            DefaultInstance.Dispose();
        }
        #endregion
    }
}
