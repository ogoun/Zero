using System;
using System.Collections.Generic;
using System.Threading;
using ZeroLevel.Services.Shedulling;

namespace ZeroLevel.Services.Cache
{
    internal sealed class TimerCachee<T>
        : IDisposable
    {
        private sealed class CacheeItem<I>
        {
            public Lazy<I> Lazy { get; set; }
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
            Lazy<T> lazy;
            lock (_cacheeLock)
            {
                if (_cachee.TryGetValue(key, out var v))
                {
                    v.LastAcessTime = DateTime.UtcNow;
                    lazy = v.Lazy;
                }
                else
                {
                    var capturedKey = key;
                    lazy = new Lazy<T>(() => _factory.Invoke(capturedKey), LazyThreadSafetyMode.ExecutionAndPublication);
                    _cachee[key] = new CacheeItem<T> { Lazy = lazy, LastAcessTime = DateTime.UtcNow };
                }
            }
            try
            {
                // factory invoked outside the cachee lock; concurrent Get on same key shares this Lazy
                return lazy.Value;
            }
            catch
            {
                // remove the broken entry so the next caller can retry
                lock (_cacheeLock)
                {
                    if (_cachee.TryGetValue(key, out var v) && ReferenceEquals(v.Lazy, lazy))
                        _cachee.Remove(key);
                }
                throw;
            }
        }

        public void Drop(string key)
        {
            CacheeItem<T> v;
            lock (_cacheeLock)
            {
                if (!_cachee.TryGetValue(key, out v)) return;
                _cachee.Remove(key);
            }
            DisposeItem(key, v, "Drop");
        }

        /// <summary>
        /// Removes the key from cache and returns its value WITHOUT invoking dispose.
        /// Caller takes ownership and is responsible for disposal.
        /// If the entry exists but its factory has not yet completed (or threw), returns false.
        /// </summary>
        public bool TryRemove(string key, out T value)
        {
            CacheeItem<T> v;
            lock (_cacheeLock)
            {
                if (!_cachee.TryGetValue(key, out v))
                {
                    value = default!;
                    return false;
                }
                _cachee.Remove(key);
            }
            // only return a value the caller can take ownership of when the factory succeeded
            if (v.Lazy.IsValueCreated)
            {
                value = v.Lazy.Value;
                return true;
            }
            value = default!;
            return false;
        }

        private void CheckAndCleacnCachee()
        {
            List<KeyValuePair<string, CacheeItem<T>>> expired = null!;
            lock (_cacheeLock)
            {
                foreach (var pair in _cachee)
                {
                    if ((DateTime.UtcNow - pair.Value.LastAcessTime) > _expirationPeriod)
                    {
                        if (expired == null!) expired = new List<KeyValuePair<string, CacheeItem<T>>>();
                        expired.Add(pair);
                    }
                }
                if (expired != null!)
                {
                    foreach (var pair in expired) _cachee.Remove(pair.Key);
                }
            }
            // dispose outside the lock — disposal of MMF/file handles can be slow
            if (expired != null!)
            {
                foreach (var pair in expired) DisposeItem(pair.Key, pair.Value, "CheckAndCleacnCachee");
            }
        }

        public void DropAll()
        {
            List<KeyValuePair<string, CacheeItem<T>>> snapshot;
            lock (_cacheeLock)
            {
                snapshot = new List<KeyValuePair<string, CacheeItem<T>>>(_cachee);
                _cachee.Clear();
            }
            foreach (var pair in snapshot) DisposeItem(pair.Key, pair.Value, "DropAll");
        }

        private void DisposeItem(string key, CacheeItem<T> item, string source)
        {
            if (_onDisposeAction == null!) return;
            if (!item.Lazy.IsValueCreated) return; // factory never completed; nothing to dispose
            try
            {
                _onDisposeAction.Invoke(item.Lazy.Value);
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[TimerCachee.{source}] Key '{key}'. Fault dispose.");
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
