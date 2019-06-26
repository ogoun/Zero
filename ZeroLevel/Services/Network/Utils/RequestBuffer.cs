using System;
using System.Collections.Generic;
using ZeroLevel.Services.Pools;

namespace ZeroLevel.Network
{
    internal sealed class RequestBuffer
    {
        private readonly object _reqeust_lock = new object();
        private Dictionary<long, RequestInfo> _requests = new Dictionary<long, RequestInfo>();
        private static ObjectPool<RequestInfo> _ri_pool = new ObjectPool<RequestInfo>(() => new RequestInfo());

        public void RegisterForFrame(int identity, Action<Frame> callback, Action<string> fail = null)
        {
            var ri = _ri_pool.Allocate();
            lock (_reqeust_lock)
            {
                ri.Reset(callback, fail);
                _requests.Add(identity, ri);
            }
        }

        public void Fail(long frameId, string message)
        {
            RequestInfo ri = null;
            lock (_reqeust_lock)
            {
                if (_requests.ContainsKey(frameId))
                {
                    ri = _requests[frameId];
                    _requests.Remove(frameId);
                }
            }
            if (ri != null)
            {
                ri.Fail(message);
                _ri_pool.Free(ri);
            }
        }

        public void Success(long frameId, Frame frame)
        {
            RequestInfo ri = null;
            lock (_reqeust_lock)
            {
                if (_requests.ContainsKey(frameId))
                {
                    ri = _requests[frameId];
                    _requests.Remove(frameId);
                }
            }
            if (ri != null)
            {
                ri.Success(frame);
                _ri_pool.Free(ri);
            }
        }

        public void StartSend(long frameId)
        {
            RequestInfo ri = null;
            lock (_reqeust_lock)
            {
                if (_requests.ContainsKey(frameId))
                {
                    ri = _requests[frameId];
                }
            }
            if (ri != null)
            {
                ri.StartSend();
            }
        }

        public void TestForTimeouts()
        {
            var now_ticks = DateTime.UtcNow.Ticks;
            var to_remove = new List<long>();
            lock (_reqeust_lock)
            {
                foreach (var pair in _requests)
                {
                    if (pair.Value.Sended == false) continue;
                    var diff = now_ticks - pair.Value.Timestamp;
                    if (diff > BaseSocket.MAX_REQUEST_TIME_TICKS)
                    {
                        to_remove.Add(pair.Key);
                    }
                }
            }
            foreach (var key in to_remove)
            {
                Fail(key, "Timeout");
            }
        }
    }
}
