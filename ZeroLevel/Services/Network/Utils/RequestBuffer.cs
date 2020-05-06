using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ZeroLevel.Network
{
    internal sealed class RequestBuffer
    {
        private ConcurrentDictionary<long, Action<byte[]>> _callbacks = new ConcurrentDictionary<long, Action<byte[]>>();
        private ConcurrentDictionary<long, Action<string>> _fallbacks = new ConcurrentDictionary<long, Action<string>>();
        private ConcurrentDictionary<long, long> _timeouts = new ConcurrentDictionary<long, long>();

        public void RegisterForFrame(int identity, Action<byte[]> callback, Action<string> fallback = null)
        {
            if (callback != null)
            {
                _callbacks.TryAdd(identity, callback);
            }
            if (fallback != null)
            {
                _fallbacks.TryAdd(identity, fallback);
            }
        }

        public void Fail(long identity, string message)
        {
            Action<string> rec;
            if (_fallbacks.TryRemove(identity, out rec))
            {
                try
                {
                    rec(message);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Fail invoke fallback for request '{identity}' with message '{message ?? string.Empty}'");
                }
                rec = null;
            }
        }

        public void Success(long identity, byte[] data)
        {
            Action<byte[]> rec;
            if (_callbacks.TryRemove(identity, out rec))
            {
                try
                {
                    rec(data);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Fail invoke callback for request '{identity}'. Response size '{data?.Length ?? 0}'");
                }
                rec = null;
            }
        }

        public void StartSend(long identity)
        {
            if (_callbacks.ContainsKey(identity) 
                || _fallbacks.ContainsKey(identity))
            {
                _timeouts.TryAdd(identity, DateTime.UtcNow.Ticks);
            }
        }

        public void Timeout(List<long> identities)
        {
            long t;
            Action<string> rec;
            foreach (var id in identities)
            {
                if (_fallbacks.TryRemove(id, out rec))
                {
                    try
                    {
                        rec("Timeout");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"Fail invoke fallback for request '{id}' by timeout");
                    }
                    rec = null;
                }
                _timeouts.TryRemove(id, out t);
            }
        }

        public void TestForTimeouts()
        {
            var now_ticks = DateTime.UtcNow.Ticks;
            var to_remove = new List<long>();
            foreach (var pair in _timeouts)
            {
                var diff = now_ticks - pair.Value;
                if (diff > BaseSocket.MAX_REQUEST_TIME_TICKS)
                {
                    to_remove.Add(pair.Key);
                }
            }
            Timeout(to_remove);
        }
    }
}
