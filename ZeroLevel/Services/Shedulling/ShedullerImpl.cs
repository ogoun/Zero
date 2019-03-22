using System;
using System.Collections.Concurrent;
using ZeroLevel.Services.Logging;

namespace ZeroLevel.Services.Shedulling
{
    /// <summary>
    /// Простой планировщик для периодических и разовых задач выполняемых по расписанию
    /// </summary>
    internal class ShedullerImpl
        : ISheduller
    {
        private readonly DateTimeSheduller _sheduller;

        public ShedullerImpl()
        {
            _sheduller = new DateTimeSheduller();
        }

        #region One time events
        public long RemindAfter(TimeSpan timespan, Action<long> callback)
        {
            return _sheduller.Push(timespan, callback);
        }

        public long RemindAt(DateTime date, Action<long> callback)
        {
            return _sheduller.Push(date, callback);
        }
        #endregion

        #region Repitable behaviour
        private readonly ConcurrentDictionary<long, ExpiredObject> _repitableActions = new ConcurrentDictionary<long, ExpiredObject>();
        /// <summary>
        /// Исполняет действие раз в период, при этом период перерасчитывается по переданной функции при каждом пересоздании задачи
        /// </summary>
        /// <param name="nextEventPeriodCalcFunction">Функция для расчета следующего периода</param>
        /// <param name="callback">Действие</param>
        /// <returns>Идентификатор задания</returns>
        public long RemindEveryNonlinearPeriod(Func<TimeSpan> nextEventPeriodCalcFunction,
            Action<long> callback,
            bool breakWherError = false)
        {
            return RemindEveryNonlinearPeriod(nextEventPeriodCalcFunction, nextEventPeriodCalcFunction, callback, breakWherError);
        }
        /// <summary>
        ///  Исполняет действие раз в период, при этом период перерасчитывается по переданной функции при каждом пересоздании задачи
        /// </summary>
        /// <param name="firstEventPeriodCalcFunction">Функция для расчета периода до первого исполнения</param>
        /// <param name="nextEventPeriodCalcFunction">Функция для расчета периода до последующих исполнений</param>
        /// <param name="callback">Действие</param>
        /// <returns>Идентификатор задания</returns>
        public long RemindEveryNonlinearPeriod(Func<TimeSpan> firstEventPeriodCalcFunction,
            Func<TimeSpan> nextEventPeriodCalcFunction,
            Action<long> callback,
            bool breakWherError = false)
        {
            var obj = new ExpiredObject { ExpirationDate = DateTime.Now.AddMilliseconds(firstEventPeriodCalcFunction().TotalMilliseconds) };
            _repitableActions.TryAdd(obj.Key, obj);
            obj.Callback = (key) =>
            {
                try
                {
                    callback(key);
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, $"[Sheduller] Fault call task '{key}' handler");
                    if (breakWherError)
                        return;
                }
                ExpiredObject repObj;
                if (_repitableActions.TryGetValue(obj.Key, out repObj))
                {
                    _sheduller.Push(repObj.Reset(DateTime.Now.AddMilliseconds(nextEventPeriodCalcFunction().TotalMilliseconds)));
                }
            };
            return _sheduller.Push(obj);
        }
        /// <summary>
        /// Исполняет действие раз в период, при этом дата перерасчитывается по переданной функции при каждом пересоздании задачи
        /// </summary>
        /// <param name="nextEventDateCalcFunction">Функция для расчета следующей даты</param>
        /// <param name="callback">Действие</param>
        /// <returns>Идентификатор задания</returns>
        public long RemindEveryNonlinearDate(Func<DateTime, DateTime> nextEventDateCalcFunction,
            Action<long> callback,
            bool breakWherError = false)
        {
            return RemindEveryNonlinearDate(nextEventDateCalcFunction, nextEventDateCalcFunction, callback, breakWherError);
        }
        public long RemindEveryNonlinearDate(DateTime firstTime,
            Func<DateTime, DateTime> nextEventDateCalcFunction,
            Action<long> callback,
            bool breakWherError = false)
        {
            var obj = new ExpiredObject { ExpirationDate = firstTime };
            _repitableActions.TryAdd(obj.Key, obj);
            obj.Callback = (key) =>
            {
                try
                {
                    callback(key);
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, $"[Sheduller] Fault call task '{key}' handler");
                    if (breakWherError)
                        return;
                }
                ExpiredObject repObj;
                if (_repitableActions.TryGetValue(obj.Key, out repObj))
                {
                    var nextDate = nextEventDateCalcFunction(obj.ExpirationDate);
                    if (DateTime.Compare(nextDate, DateTime.MinValue) == 0)
                    {
                        Remove(repObj.Key);
                    }
                    else
                    {
                        _sheduller.Push(repObj.Reset(nextDate));
                    }
                }
            };
            return _sheduller.Push(obj);
        }
        /// <summary>
        /// Исполняет действие раз в период, при этом дата перерасчитывается по переданной функции при каждом пересоздании задачи
        /// </summary>
        /// <param name="firstEventDateCalcFunction">Функция для расчет даты первого запуска</param>
        /// <param name="nextEventDateCalcFunction">Функция для расчета следующей даты</param>
        /// <param name="callback">Действие</param>
        /// <returns>Идентификатор задания</returns>
        public long RemindEveryNonlinearDate(Func<DateTime, DateTime> firstEventDateCalcFunction,
            Func<DateTime, DateTime> nextEventDateCalcFunction,
            Action<long> callback,
            bool breakWherError = false)
        {
            var obj = new ExpiredObject { ExpirationDate = firstEventDateCalcFunction(DateTime.Now) };
            _repitableActions.TryAdd(obj.Key, obj);
            obj.Callback = (key) =>
            {
                try
                {
                    callback(key);
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, $"[Sheduller] Fault call task '{key}' handler");
                    if (breakWherError)
                        return;
                }
                ExpiredObject repObj;
                if (_repitableActions.TryGetValue(obj.Key, out repObj))
                {
                    var nextDate = nextEventDateCalcFunction(obj.ExpirationDate);
                    if (DateTime.Compare(nextDate, DateTime.MinValue) == 0)
                    {
                        Remove(repObj.Key);
                    }
                    else
                    {
                        _sheduller.Push(repObj.Reset(nextDate));
                    }
                }
            };
            return _sheduller.Push(obj);
        }
        /// <summary>
        /// Исполняет действие раз в указанный период
        /// </summary>
        /// <param name="timespan">Период</param>
        /// <param name="callback">Действие</param>
        /// <returns>Идентификатор задания</returns>
        public long RemindEvery(TimeSpan timespan,
            Action<long> callback,
            bool breakWherError = false)
        {
            return RemindEvery(timespan, timespan, callback, breakWherError);
        }
        /// <summary>
        /// Исполняет действие раз в указанный период
        /// </summary>
        /// <param name="first">Период до первого выполнения</param>
        /// <param name="next">Период</param>
        /// <param name="callback">Действие</param>
        /// <returns>Идентификатор задания</returns>
        public long RemindEvery(TimeSpan first,
            TimeSpan next,
            Action<long> callback,
            bool breakWherError = false)
        {
            var obj = new ExpiredObject { ExpirationDate = DateTime.Now.AddMilliseconds(first.TotalMilliseconds) };
            _repitableActions.TryAdd(obj.Key, obj);
            obj.Callback = (key) =>
            {
                try
                {
                    callback(key);
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, $"[Sheduller] Fault call task '{key}' handler");
                    if (breakWherError)
                        return;
                }
                ExpiredObject repObj;
                if (_repitableActions.TryGetValue(obj.Key, out repObj))
                {
                    _sheduller.Push(repObj.Reset(DateTime.Now.AddMilliseconds(next.TotalMilliseconds)));
                }
            };
            return _sheduller.Push(obj);
        }
        public long RemindWhile(TimeSpan period,
            Func<long, bool> callback,
            Action continueWith = null,
            bool breakWherError = false)
        {
            var obj = new ExpiredObject { ExpirationDate = DateTime.Now.AddMilliseconds(period.TotalMilliseconds) };
            _repitableActions.TryAdd(obj.Key, obj);
            obj.Callback = (key) =>
            {
                bool success = false;
                try
                {
                    success = callback(key);
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, $"[Sheduller] Fault call task '{key}' handler");
                    if (breakWherError)
                        return;
                }
                if (success)
                {
                    Remove(obj.Key);
                    if (continueWith != null)
                        continueWith();
                }
                else
                {
                    ExpiredObject repObj;
                    if (_repitableActions.TryGetValue(obj.Key, out repObj))
                    {
                        _sheduller.Push(repObj.Reset(DateTime.Now.AddMilliseconds(period.TotalMilliseconds)));
                    }
                }
            };
            return _sheduller.Push(obj);
        }
        #endregion

        #region Sheduller control
        public void Pause()
        {
            _sheduller.Pause();
        }

        public void Resume()
        {
            _sheduller.Resume();
        }

        public void Clean()
        {
            _sheduller.Clean();
        }

        public bool Remove(long key)
        {
            var success = _sheduller.Remove(key);
            if (_repitableActions.ContainsKey(key))
            {
                ExpiredObject rem;
                return _repitableActions.TryRemove(key, out rem);
            }
            return success;
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _sheduller.Dispose();
            }
        }
        #endregion
    }
}
