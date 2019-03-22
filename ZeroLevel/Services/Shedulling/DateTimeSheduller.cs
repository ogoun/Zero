using System;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroLevel.Services.Shedulling
{
    public class DateTimeSheduller
           : IExpirationSheduller
    {
        private Timer _timer;
        private ExpiredObject _head = null;
        private readonly object _rw_lock = new object();
        private volatile bool _stopped = false;

        #region Ctor
        public DateTimeSheduller()
        {
            _timer = new Timer(TimerCallbackHandler, null, Timeout.Infinite, Timeout.Infinite);
        }
        #endregion

        private void TimerCallbackHandler(object state)
        {
            // POP
            ExpiredObject result = null;
            lock (_rw_lock)
            {
                if (null != _head)
                {
                    if ((_head.ExpirationDate - DateTime.Now).Ticks > 0)
                    {
                        // Защита на случай если callback был вызван, но до захвата блокировки в нем, она была
                        // захвачена другим методом, в этом случае есть риск получить на head дату истечения позже текущего времени.

                        // При изменении времени системы может быть ситуация, при которой в head лежит элемент для которого сработал таймер,
                        // но время истечения сместилось, поэтому вызов пересоздания таймера необходим
                        ResetTimer();
                        return;
                    }
                    // SWAP
                    result = _head;
                    _head = _head.Next;
                    ResetTimer();
                }
            }
            if (result != null)
            {
                Task.Run(() => result.Callback(result.Key)).ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        Exception ex = t.Exception;
                        while (ex is AggregateException && ex.InnerException != null)
                            ex = ex.InnerException;
                        Log.SystemError(ex, $"Fault task '{result.Key}' on expiration date '{result.ExpirationDate.ToString("yyyy-MM-dd HH:mm:ss fff}")}'");
                    }
                });
            }
        }

        internal long Push(ExpiredObject insert)
        {
            if (insert == null)
                throw new ArgumentNullException(nameof(insert));
            lock (_rw_lock)
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
                    ExpiredObject prev = null;
                    do
                    {
                        if ((cursor.ExpirationDate - insert.ExpirationDate).Ticks > 0)
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
            }
            return insert.Key;
        }

        public bool Remove(long key)
        {
            lock (_rw_lock)
            {
                if (_head != null)
                {
                    ExpiredObject previous, current;
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
            }
            return false;
        }

        #region API
        public long Push(TimeSpan timespan, Action<long> callback)
        {
            return Push(new ExpiredObject { Callback = callback, ExpirationDate = DateTime.Now.AddMilliseconds(timespan.TotalMilliseconds) });
        }

        public long Push(DateTime date, Action<long> callback)
        {
            return Push(new ExpiredObject { Callback = callback, ExpirationDate = date });
        }

        private void DisableTimer()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public void Pause()
        {
            lock (_rw_lock)
            {
                _stopped = true;
                DisableTimer();
            }
        }

        public void Resume()
        {
            lock (_rw_lock)
            {
                _stopped = false;
                ResetTimer();
            }
        }

        public void Clean()
        {
            lock (_rw_lock)
            {
                DisableTimer();
                _head = null;
            }
        }
        #endregion

        #region Control
        private void FindTaskByKeyWithPreviousTask(long key, out ExpiredObject previous, out ExpiredObject current)
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
                        var _big_interval_waiting_obj = new ExpiredObject(true)
                        {
                            ExpirationDate = DateTime.Now.AddMilliseconds(_max_interval),
                            Callback = (key) =>
                            {
                                lock (_rw_lock)
                                {
                                    ResetTimer();
                                    _head = null;
                                }
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
        #endregion
    }
}
