using System;
using System.Collections.Concurrent;

namespace ZeroLevel.Services.Shedulling
{
    /// <summary>
    /// Simple scheduler for periodic and one-time scheduled tasks
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

        #endregion One time events

        #region Repitable behaviour

        private readonly ConcurrentDictionary<long, ExpiredObject> _repitableActions = new ConcurrentDictionary<long, ExpiredObject>();

        public DateTime this[long index]
        {
            get
            {
                if (_repitableActions.TryGetValue(index, out var result))
                {
                    return result.ExpirationDate;
                }
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// Performs an action once a period, while the period is recalculated according to the transferred function at each re-creation of the task.
        /// </summary>
        /// <param name="nextEventPeriodCalcFunction">Function to calculate the next period</param>
        /// <returns>Task ID</returns>
        public long RemindEveryNonlinearPeriod(Func<TimeSpan> nextEventPeriodCalcFunction,
            Action<long> callback,
            bool breakWherError = false)
        {
            return RemindEveryNonlinearPeriod(nextEventPeriodCalcFunction, nextEventPeriodCalcFunction, callback, breakWherError);
        }

        /// <summary>
        ///  Performs an action once a period, while the period is recalculated according to the transferred function at each re-creation of the task.
        /// </summary>
        /// <param name="firstEventPeriodCalcFunction">The function to calculate the period to the first execution</param>
        /// <param name="nextEventPeriodCalcFunction">The function for calculating the period until subsequent performances</param>
        /// <returns>Task ID</returns>
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
        /// Performs an action once per period, while the date is recalculated from the function transferred each time the task is recreated.
        /// </summary>
        /// <param name="nextEventDateCalcFunction">The function to calculate the next date</param>
        /// <param name="callback">Action</param>
        /// <returns>Task ID</returns>
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
        /// Performs an action once per period, while the date is recalculated from the function transferred each time the task is recreated.
        /// </summary>
        /// <param name="firstEventDateCalcFunction">The function to calculate the first run date</param>
        /// <param name="nextEventDateCalcFunction">The function to calculate the next date</param>
        /// <param name="callback">Action</param>
        /// <returns>Task ID</returns>
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
        /// Performs an action once in a specified period
        /// </summary>
        /// <param name="timespan">Period</param>
        /// <param name="callback">Action</param>
        /// <returns>Task ID</returns>
        public long RemindEvery(TimeSpan timespan,
            Action<long> callback,
            bool breakWherError = false)
        {
            return RemindEvery(timespan, timespan, callback, breakWherError);
        }

        /// <summary>
        /// Performs an action once in a specified period
        /// </summary>
        /// <param name="first">Period to first run</param>
        /// <param name="next">Period</param>
        /// <param name="callback">Action</param>
        /// <returns>Task ID</returns>
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

        #endregion Repitable behaviour

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

        public void SetInitialIndex(long index)
        {
            ExpiredObject.ResetIndex(index);
        }
        #endregion Sheduller control

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
        #endregion IDisposable
    }
}