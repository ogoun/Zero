using System;
using System.Collections.Generic;
using ZeroLevel.Services.Shedulling;

namespace ZeroLevel.Services.Cache
{
    internal sealed class TimerCachee<T>
        : IDisposable
    {
        private sealed class CacheeItem<I>
        {
            public I Value { get; set; }
            public DateTime LastAcessTime { get; set; }
        }

        private readonly IDictionary<string, CacheeItem<T>> _cachee;
        private readonly object _cacheeLock = new object();

        private readonly ISheduller _sheduller;
        private readonly Func<string, T> _factory;
        private readonly Action<T> _onDisposeAction;
        private readonly TimeSpan _expirationPeriod;
        public TimerCachee(TimeSpan expirationPeriod, Func<string, T> factory, Action<T> onDisposeAction, int capacity = 512)
        {
            _factory = factory;
            _onDisposeAction = onDisposeAction;
            _sheduller = Sheduller.Create();
            _cachee = new Dictionary<string, CacheeItem<T>>(capacity);
            _expirationPeriod = expirationPeriod;
            var ts = TimeSpan.FromSeconds(60);
            if (ts.TotalSeconds > expirationPeriod.TotalSeconds)
            {
                ts = expirationPeriod;
            }
            _sheduller.RemindEvery(ts, _ => CheckAndCleacnCachee());
        }

        public T Get(string key)
        {
            lock (_cacheeLock)
            {
                if (_cachee.TryGetValue(key, out var v))
                {
                    v.LastAcessTime = DateTime.UtcNow;
                    return v.Value;
                }
            }
            var obj = _factory.Invoke(key);
            var item = new CacheeItem<T> { Value = obj, LastAcessTime = DateTime.UtcNow };
            lock (_cacheeLock)
            {
                _cachee[key] = item;
            }
            return obj;
        }

        public void Drop(string key)
        {
            lock (_cacheeLock)
            {
                if (_cachee.TryGetValue(key, out var v))
                {
                    try
                    {
                        if (_onDisposeAction != null)
                        {
                            _onDisposeAction.Invoke(v.Value);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.SystemError(ex, $"[TimerCachee.Drop] Key '{key}'. Fault dispose.");
                    }
                    _cachee.Remove(key);
                }
            }
        }

        private void CheckAndCleacnCachee()
        {
            lock (_cacheeLock)
            {
                var keysToRemove = new List<string>(_cachee.Count);
                foreach (var pair in _cachee)
                {
                    if ((DateTime.UtcNow - pair.Value.LastAcessTime) > _expirationPeriod)
                    {
                        keysToRemove.Add(pair.Key);
                    }
                }
                foreach (var key in keysToRemove)
                {
                    try
                    {
                        if (_onDisposeAction != null)
                        {
                            _onDisposeAction.Invoke(_cachee[key].Value);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.SystemError(ex, $"[TimerCachee.CheckAndCleacnCachee] Key '{key}'");
                    }
                    _cachee.Remove(key);
                }
            }
        }

        public void DropAll()
        {
            lock (_cacheeLock)
            {
                foreach (var pair in _cachee)
                {
                    try
                    {
                        if (_onDisposeAction != null)
                        {
                            _onDisposeAction.Invoke(pair.Value.Value);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.SystemError(ex, $"[TimerCachee.DropAll] Key '{pair.Key}'");
                    }
                }
                _cachee.Clear();
            }
        }
        public void Dispose()
        {
            _sheduller?.Clean();
            _sheduller?.Dispose();
            DropAll();
        }
    }
}
