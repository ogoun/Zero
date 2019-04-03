using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace ZeroLevel.Services.Shedulling
{
    internal class AsyncShedullerImpl
        : IAsyncSheduller
    {
        private readonly DateTimeAsyncSheduller _asyncSheduller;

        public AsyncShedullerImpl()
        {
            _asyncSheduller = new DateTimeAsyncSheduller();
        }

        #region One time events

        public long RemindAsyncAfter(TimeSpan timespan, Func<long, Task> callback)
        {
            return _asyncSheduller.Push(timespan, callback);
        }

        public long RemindAsyncAt(DateTime date, Func<long, Task> callback)
        {
            return _asyncSheduller.Push(date, callback);
        }

        #endregion One time events

        #region Repitable behaviour

        private readonly ConcurrentDictionary<long, ExpiredAsyncObject> _repitableAsyncActions = new ConcurrentDictionary<long, ExpiredAsyncObject>();

        /// <summary>
        /// Performs an action once a period, while the period is recalculated according to the transferred function at each re-creation of the task.
        /// </summary>
        /// <param name="nextEventPeriodCalcFunction">Function to calculate the next period</param>
        /// <param name="callback">Action</param>
        /// <returns>Task ID</returns>
        public long RemindAsyncEveryNonlinearPeriod(Func<TimeSpan> nextEventPeriodCalcFunction,
            Func<long, Task> callback,
            bool breakWherError = false)
        {
            return RemindAsyncEveryNonlinearPeriod(nextEventPeriodCalcFunction, nextEventPeriodCalcFunction, callback, breakWherError);
        }

        /// <summary>
        ///  Performs an action once a period, while the period is recalculated according to the transferred function at each re-creation of the task.
        /// </summary>
        /// <param name="firstEventPeriodCalcFunction">The function to calculate the period to the first execution</param>
        /// <param name="nextEventPeriodCalcFunction">The function for calculating the period until subsequent performances</param>
        /// <param name="callback">Action</param>
        /// <returns>Task ID</returns>
        public long RemindAsyncEveryNonlinearPeriod(Func<TimeSpan> firstEventPeriodCalcFunction,
            Func<TimeSpan> nextEventPeriodCalcFunction,
            Func<long, Task> callback,
            bool breakWherError = false)
        {
            var obj = new ExpiredAsyncObject { ExpirationDate = DateTime.Now.AddMilliseconds(firstEventPeriodCalcFunction().TotalMilliseconds) };
            _repitableAsyncActions.TryAdd(obj.Key, obj);
            obj.Callback = async (key) =>
            {
                try
                {
                    await callback(key);
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, $"[Sheduller] Fault call async task '{key}' handler");
                    if (breakWherError)
                        return;
                }
                ExpiredAsyncObject repObj;
                if (_repitableAsyncActions.TryGetValue(obj.Key, out repObj))
                {
                    _asyncSheduller.Push(repObj.Reset(DateTime.Now.AddMilliseconds(nextEventPeriodCalcFunction().TotalMilliseconds)));
                }
            };
            return _asyncSheduller.Push(obj);
        }

        /// <summary>
        /// Performs an action once per period, while the date is recalculated from the function transferred each time the task is recreated.
        /// </summary>
        /// <param name="nextEventDateCalcFunction">The function to calculate the next date</param>
        /// <param name="callback">Action</param>
        /// <returns>Task ID</returns>
        public long RemindAsyncEveryNonlinearDate(Func<DateTime, DateTime> nextEventDateCalcFunction,
            Func<long, Task> callback,
            bool breakWherError = false)
        {
            return RemindAsyncEveryNonlinearDate(nextEventDateCalcFunction, nextEventDateCalcFunction, callback, breakWherError);
        }

        public long RemindAsyncEveryNonlinearDate(DateTime firstTime,
            Func<DateTime, DateTime> nextEventDateCalcFunction,
            Func<long, Task> callback,
            bool breakWherError = false)
        {
            var obj = new ExpiredAsyncObject { ExpirationDate = firstTime };
            _repitableAsyncActions.TryAdd(obj.Key, obj);
            obj.Callback = async (key) =>
            {
                try
                {
                    await callback(key);
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, $"[Sheduller] Fault call async task '{key}' handler");
                    if (breakWherError)
                        return;
                }
                ExpiredAsyncObject repObj;
                if (_repitableAsyncActions.TryGetValue(obj.Key, out repObj))
                {
                    var nextDate = nextEventDateCalcFunction(obj.ExpirationDate);
                    if (DateTime.Compare(nextDate, DateTime.MinValue) == 0)
                    {
                        Remove(repObj.Key);
                    }
                    else
                    {
                        _asyncSheduller.Push(repObj.Reset(nextDate));
                    }
                }
            };
            return _asyncSheduller.Push(obj);
        }

        /// <summary>
        /// Performs an action once per period, while the date is recalculated from the function transferred each time the task is recreated.
        /// </summary>
        /// <param name="firstEventDateCalcFunction">The function to calculate the first run date</param>
        /// <param name="nextEventDateCalcFunction">The function to calculate the next date</param>
        /// <param name="callback">Action</param>
        /// <returns>Task ID</returns>
        public long RemindAsyncEveryNonlinearDate(Func<DateTime, DateTime> firstEventDateCalcFunction,
            Func<DateTime, DateTime> nextEventDateCalcFunction,
            Func<long, Task> callback,
            bool breakWherError = false)
        {
            var obj = new ExpiredAsyncObject { ExpirationDate = firstEventDateCalcFunction(DateTime.Now) };
            _repitableAsyncActions.TryAdd(obj.Key, obj);
            obj.Callback = async (key) =>
            {
                try
                {
                    await callback(key);
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, $"[Sheduller] Fault call async task '{key}' handler");
                    if (breakWherError)
                        return;
                }
                ExpiredAsyncObject repObj;
                if (_repitableAsyncActions.TryGetValue(obj.Key, out repObj))
                {
                    var nextDate = nextEventDateCalcFunction(obj.ExpirationDate);
                    if (DateTime.Compare(nextDate, DateTime.MinValue) == 0)
                    {
                        Remove(repObj.Key);
                    }
                    else
                    {
                        _asyncSheduller.Push(repObj.Reset(nextDate));
                    }
                }
            };
            return _asyncSheduller.Push(obj);
        }

        /// <summary>
        /// Performs an action once in a specified period
        /// </summary>
        /// <param name="timespan">Period</param>
        /// <param name="callback">Action</param>
        /// <returns>Task ID</returns>
        public long RemindAsyncEvery(TimeSpan timespan,
            Func<long, Task> callback,
            bool breakWherError = false)
        {
            return RemindAsyncEvery(timespan, timespan, callback, breakWherError);
        }

        /// <summary>
        /// Performs an action once in a specified period
        /// </summary>
        /// <param name="first">Period to first run</param>
        /// <param name="next">Period</param>
        /// <param name="callback">Action</param>
        /// <returns>Task ID</returns>
        public long RemindAsyncEvery(TimeSpan first,
            TimeSpan next,
            Func<long, Task> callback,
            bool breakWherError = false)
        {
            var obj = new ExpiredAsyncObject { ExpirationDate = DateTime.Now.AddMilliseconds(first.TotalMilliseconds) };
            _repitableAsyncActions.TryAdd(obj.Key, obj);
            obj.Callback = async (key) =>
            {
                try
                {
                    await callback(key).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, $"[Sheduller] Fault call async task '{key}' handler");
                    if (breakWherError)
                        return;
                }
                ExpiredAsyncObject repObj;
                if (_repitableAsyncActions.TryGetValue(obj.Key, out repObj))
                {
                    _asyncSheduller.Push(repObj.Reset(DateTime.Now.AddMilliseconds(next.TotalMilliseconds)));
                }
            };
            return _asyncSheduller.Push(obj);
        }

        public long RemindAsyncWhile(TimeSpan period,
            Func<long, Task<bool>> callback,
            Action continueWith = null,
            bool breakWherError = false)
        {
            var obj = new ExpiredAsyncObject { ExpirationDate = DateTime.Now.AddMilliseconds(period.TotalMilliseconds) };
            _repitableAsyncActions.TryAdd(obj.Key, obj);
            obj.Callback = async (key) =>
            {
                bool success = false;
                try
                {
                    success = await callback(key);
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, $"[Sheduller] Fault call async task '{key}' handler");
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
                    ExpiredAsyncObject repObj;
                    if (_repitableAsyncActions.TryGetValue(obj.Key, out repObj))
                    {
                        _asyncSheduller.Push(repObj.Reset(DateTime.Now.AddMilliseconds(period.TotalMilliseconds)));
                    }
                }
            };
            return _asyncSheduller.Push(obj);
        }

        #endregion Repitable behaviour

        #region Sheduller control

        public void Pause()
        {
            _asyncSheduller.Pause();
        }

        public void Resume()
        {
            _asyncSheduller.Resume();
        }

        public void Clean()
        {
            _asyncSheduller.Clean();
        }

        public bool Remove(long key)
        {
            var success = _asyncSheduller.Remove(key);
            if (_repitableAsyncActions.ContainsKey(key))
            {
                ExpiredAsyncObject rem;
                return _repitableAsyncActions.TryRemove(key, out rem);
            }
            return success;
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
                _asyncSheduller.Dispose();
            }
        }

        #endregion IDisposable
    }
}