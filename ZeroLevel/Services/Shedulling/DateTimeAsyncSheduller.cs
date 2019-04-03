using System;
using System.Threading;
using System.Threading.Tasks;
using ZeroLevel.Services.Async;

namespace ZeroLevel.Services.Shedulling
{
    public class DateTimeAsyncSheduller
           : IExpirationAsyncSheduller
    {
        private Timer _timer;
        private ExpiredAsyncObject _head = null;
        private AsyncLock _lock = new AsyncLock();
        private volatile bool _stopped = false;

        #region Ctor

        public DateTimeAsyncSheduller()
        {
            _timer = new Timer(TimerCallbackHandler, null, Timeout.Infinite, Timeout.Infinite);
        }

        #endregion Ctor

        private void TimerCallbackHandler(object state)
        {
            // POP
            ExpiredAsyncObject result = null;
            if (null != _head)
            {
                if (DateTime.Compare(_head.ExpirationDate, DateTime.Now) > 0)
                {
                    ResetTimer();
                    return;
                }
                // SWAP
                result = _head;
                _head = _head.Next;
                ResetTimer();
            }
            if (result != null)
            {
                result.Callback(result.Key).ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        Exception ex = t.Exception;
                        while (ex is AggregateException && ex.InnerException != null)
                            ex = ex.InnerException;
                        Log.SystemError(ex, "Fault task '{0}' on expiration date '{1:yyyy-MM-dd HH:mm:ss fff}'", result.Key, result.ExpirationDate);
                    }
                });
            }
        }

        internal long Push(ExpiredAsyncObject insert)
        {
            if (insert == null)
                throw new ArgumentNullException(nameof(insert));
            using (_lock.Lock())
            {
                if (null == _head)
                {
                    _head = insert;
                    ResetTimer();
                }
                else
                {
                    var cursor = _head;
                    var reset = false;
                    if (cursor.Key == -1) // if system task for task with big interval (> 2^32 - 2 ms)
                    {
                        DisableTimer();
                        _head = _head.Next; // remove system task from head
                        cursor = _head;
                        reset = true;
                    }
                    ExpiredAsyncObject prev = null;
                    do
                    {
                        if (DateTime.Compare(cursor.ExpirationDate, insert.ExpirationDate) > 0)
                        {
                            insert.Next = cursor;
                            if (null == prev) // insert to head
                            {
                                _head = insert;
                                ResetTimer();
                                reset = false;
                            }
                            else
                            {
                                prev.Next = insert;
                            }
                            break;
                        }
                        prev = cursor;
                        cursor = cursor.Next;
                        if (cursor == null)
                        {
                            prev.Next = insert;
                        }
                    } while (cursor != null);
                    if (reset)
                    {
                        ResetTimer();
                    }
                }
                _lock.ReleaseLock();
            }
            return insert.Key;
        }

        public bool Remove(long key)
        {
            using (_lock.Lock())
            {
                if (_head != null)
                {
                    ExpiredAsyncObject previous, current;
                    FindTaskByKeyWithPreviousTask(key, out previous, out current);
                    if (current != null)
                    {
                        if (_head.Key == current.Key)
                        {
                            _head = _head.Next;
                            ResetTimer();
                        }
                        else
                        {
                            previous.Next = current.Next;
                        }
                        return true;
                    }
                }
                _lock.ReleaseLock();
            }
            return false;
        }

        #region API

        public long Push(TimeSpan timespan, Func<long, Task> callback)
        {
            return Push(new ExpiredAsyncObject { Callback = callback, ExpirationDate = DateTime.Now.AddMilliseconds(timespan.TotalMilliseconds) });
        }

        public long Push(DateTime date, Func<long, Task> callback)
        {
            return Push(new ExpiredAsyncObject { Callback = callback, ExpirationDate = date });
        }

        private void DisableTimer()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public void Pause()
        {
            using (_lock.Lock())
            {
                _stopped = true;
                DisableTimer();
                _lock.ReleaseLock();
            }
        }

        public void Resume()
        {
            using (_lock.Lock())
            {
                _stopped = false;
                ResetTimer();
                _lock.ReleaseLock();
            }
        }

        public void Clean()
        {
            using (_lock.Lock())
            {
                DisableTimer();
                _head = null;
                _lock.ReleaseLock();
            }
        }

        #endregion API

        #region Control

        private void FindTaskByKeyWithPreviousTask(long key, out ExpiredAsyncObject previous, out ExpiredAsyncObject current)
        {
            if (_head.Key == key)
            {
                previous = null;
                current = _head;
                return;
            }
            var cursor = _head.Next;
            var prev = _head;
            while (cursor != null)
            {
                if (cursor.Key == key)
                {
                    previous = prev;
                    current = cursor;
                    return;
                }
                prev = cursor;
                cursor = cursor.Next;
            }
            previous = null;
            current = null;
            return;
        }

        private const uint _max_interval = 4294967294;
        private static readonly TimeSpan _infinite = TimeSpan.FromMilliseconds(Timeout.Infinite);

        private void ResetTimer()
        {
            if (_timer != null)
            {
                if (null != _head && _stopped == false)
                {
                    var diff = (_head.ExpirationDate - DateTime.Now);
                    if (diff.TotalMilliseconds > _max_interval)
                    {
                        var _big_interval_waiting_obj = new ExpiredAsyncObject(true)
                        {
                            ExpirationDate = DateTime.Now.AddMilliseconds(_max_interval),
                            Callback = (key) =>
                            {
                                using (_lock.Lock())
                                {
                                    ResetTimer();
                                    _head = null;
                                    _lock.ReleaseLock();
                                }
                                return Task.CompletedTask;
                            },
                            Next = _head
                        };
                        _head = _big_interval_waiting_obj;
                        _timer.Change(_max_interval, Timeout.Infinite);
                    }
                    else
                    {
                        if (diff.Ticks < 0)
                        {
                            diff = TimeSpan.Zero;
                        }
                        _timer.Change(diff, _infinite);
                    }
                }
                else
                {
                    DisableTimer();
                }
            }
        }

        #endregion Control

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
                if (_timer != null)
                {
                    Clean();
                    if (null != _timer)
                    {
                        _timer.Dispose();
                        _timer = null;
                    }
                }
            }
        }

        #endregion IDisposable
    }
}