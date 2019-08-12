using System;
using System.Collections.Generic;
using System.Threading;
using ZeroLevel.Services.Pools;

namespace ZeroLevel.Network
{
    internal sealed class RequestBuffer
    {
        private SpinLock _reqeust_lock = new SpinLock();
        private Dictionary<long, RequestInfo> _requests = new Dictionary<long, RequestInfo>();
        private static ObjectPool<RequestInfo> _ri_pool = new ObjectPool<RequestInfo>(() => new RequestInfo());

        public void RegisterForFrame(int identity, Action<byte[]> callback, Action<string> fail = null)
        {
            var ri = _ri_pool.Allocate();
            bool take = false;
            try
            {
                _reqeust_lock.Enter(ref take);
                ri.Reset(callback, fail);
                _requests.Add(identity, ri);
            }
            finally
            {
                if (take) _reqeust_lock.Exit(false);
            }
        }

        public void Fail(long frameId, string message)
        {
            RequestInfo ri = null;
            bool take = false;
            try
            {
                _reqeust_lock.Enter(ref take);
                if (_requests.ContainsKey(frameId))
                {
                    ri = _requests[frameId];
                    _requests.Remove(frameId);
                }
            }
            finally
            {
                if (take) _reqeust_lock.Exit(false);
            }
            if (ri != null)
            {
                ri.Fail(message);
                _ri_pool.Free(ri);
            }
        }

        public void Success(long frameId, byte[] data)
        {
            RequestInfo ri = null;
            bool take = false;
            try
            {
                _reqeust_lock.Enter(ref take);
                if (_requests.ContainsKey(frameId))
                {
                    ri = _requests[frameId];
                    _requests.Remove(frameId);
                }
            }
            finally
            {
                if (take) _reqeust_lock.Exit(false);
            }
            if (ri != null)
            {
                ri.Success(data);
                _ri_pool.Free(ri);
            }
        }

        public void StartSend(long frameId)
        {
            RequestInfo ri = null;
            bool take = false;
            try
            {
                _reqeust_lock.Enter(ref take);
                if (_requests.ContainsKey(frameId))
                {
                    ri = _requests[frameId];
                }
            }
            finally
            {
                if (take) _reqeust_lock.Exit(false);
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
            bool take = false;
            try
            {
                _reqeust_lock.Enter(ref take);
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
            finally
            {
                if (take) _reqeust_lock.Exit(false);
            }
            foreach (var key in to_remove)
            {
                Fail(key, "Timeout");
            }
        }
    }
}
