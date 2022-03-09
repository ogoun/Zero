using MemoryPools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ZeroLevel.Network
{
    internal sealed class RequestBuffer
    {
        private ConcurrentDictionary<long, RequestInfo> _requests = new ConcurrentDictionary<long, RequestInfo>();
        private static DefaultObjectPool<RequestInfo> _ri_pool = new DefaultObjectPool<RequestInfo>(new DefaultPooledObjectPolicy<RequestInfo>());

        public void RegisterForFrame(int identity, Action<byte[]> callback, Action<string> fail = null)
        {
            var ri = _ri_pool.Get();
            ri.Reset(callback, fail);
            _requests[identity] = ri;
        }

        public void Fail(long frameId, string message)
        {
            RequestInfo ri;
            if (_requests.TryRemove(frameId, out ri))
            {
                ri.Fail(message);
                _ri_pool.Return(ri);
            }
        }

        public void Success(long frameId, byte[] data)
        {
            RequestInfo ri;
            if (_requests.TryRemove(frameId, out ri))
            {
                ri.Success(data);
                _ri_pool.Return(ri);
            }
        }

        public void StartSend(long frameId)
        {
            RequestInfo ri;
            if (_requests.TryGetValue(frameId, out ri))
            {
                ri.StartSend();
            }
        }

        public void Timeout(List<long> frameIds)
        {
            RequestInfo ri;
            for (int i = 0; i < frameIds.Count; i++)
            {
                if (_requests.TryRemove(frameIds[i], out ri))
                {
                    _ri_pool.Return(ri);
                }
            }
        }

        public void TestForTimeouts()
        {
            var now_ticks = DateTime.UtcNow.Ticks;
            var to_remove = new List<long>();
            foreach (var pair in _requests)
            {
                if (pair.Value.Sended == false) continue;
                var diff = now_ticks - pair.Value.Timestamp;
                if (diff > BaseSocket.MAX_REQUEST_TIME_TICKS)
                {
                    to_remove.Add(pair.Key);
                }
            }
            Timeout(to_remove);
        }
    }
}
