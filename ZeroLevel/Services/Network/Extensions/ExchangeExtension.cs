using System;
using System.Threading;
using ZeroLevel.Services.Pools;

namespace ZeroLevel.Network
{
    public static class ExchangeExtension
    {
        static ObjectPool<AutoResetEvent> _mrePool = new ObjectPool<AutoResetEvent>(() => new AutoResetEvent(false), 16);

        public static Tresponse Request<Tresponse>(this IClientSet exchange, string alias, TimeSpan timeout)
        {
            Tresponse response = default;
            var ev = _mrePool.Allocate();
            try
            {
                if (exchange.Request<Tresponse>(alias, 
                    _response => { response = _response; ev.Set(); }))
                {
                    ev.WaitOne(timeout);
                }
            }
            finally
            {
                _mrePool.Free(ev);
            }
            return response;
        }

        public static Tresponse Request<Tresponse>(this IClientSet exchange, string alias, string inbox, TimeSpan timeout)
        {
            Tresponse response = default;
            var ev = _mrePool.Allocate();
            try
            {
                if (exchange.Request<Tresponse>(alias, inbox, 
                    _response => { 
                        response = _response; 
                        ev.Set(); 
                    }))
                {
                    ev.WaitOne(timeout);
                }
            }
            finally
            {
                _mrePool.Free(ev);
            }
            return response;
        }

        public static Tresponse Request<Trequest, Tresponse>(this IClientSet exchange, string alias, Trequest request, TimeSpan timeout)
        {
            Tresponse response = default;
            var ev = _mrePool.Allocate();
            try
            {
                if (exchange.Request<Trequest, Tresponse>(alias, request,
                    _response => { response = _response; ev.Set(); }))
                {
                    ev.WaitOne(timeout);
                }
            }
            finally
            {
                _mrePool.Free(ev);
            }
            return response;
        }

        public static Tresponse Request<Trequest, Tresponse>(this IClientSet exchange, string alias, string inbox
            , Trequest request, TimeSpan timeout)
        { 
            Tresponse response = default;
            var ev = _mrePool.Allocate();
            try
            {
                if (exchange.Request<Trequest, Tresponse>(alias, inbox, request, 
                    _response => { response = _response; ev.Set(); }))
                {
                    ev.WaitOne(timeout);
                }
            }
            finally
            {
                _mrePool.Free(ev);
            }
            return response;
        }

        public static Tresponse Request<Tresponse>(this IClientSet exchange, string alias)
        {
            Tresponse response = default;
            var ev = _mrePool.Allocate();
            try
            {
                if (exchange.Request<Tresponse>(alias,
                    _response => { response = _response; ev.Set(); }))
                {
                    ev.WaitOne(Network.BaseSocket.MAX_REQUEST_TIME_MS);
                }
            }
            finally
            {
                _mrePool.Free(ev);
            }
            return response;
        }

        public static Tresponse Request<Tresponse>(this IClientSet exchange, string alias, string inbox)
        {
            Tresponse response = default;
            var ev = _mrePool.Allocate();
            try
            {
                if (exchange.Request<Tresponse>(alias, inbox,
                    _response => {
                        response = _response;
                        ev.Set();
                    }))
                {
                    ev.WaitOne(Network.BaseSocket.MAX_REQUEST_TIME_MS);
                }
            }
            finally
            {
                _mrePool.Free(ev);
            }
            return response;
        }

        public static Tresponse Request<Trequest, Tresponse>(this IClientSet exchange, string alias, Trequest request)
        {
            Tresponse response = default;
            var ev = _mrePool.Allocate();
            try
            {
                if (exchange.Request<Trequest, Tresponse>(alias, request,
                    _response => { response = _response; ev.Set(); }))
                {
                    ev.WaitOne(Network.BaseSocket.MAX_REQUEST_TIME_MS);
                }
            }
            finally
            {
                _mrePool.Free(ev);
            }
            return response;
        }

        public static Tresponse Request<Trequest, Tresponse>(this IClientSet exchange, string alias, string inbox
            , Trequest request)
        {
            Tresponse response = default;
            var ev = _mrePool.Allocate();
            try
            {
                if (exchange.Request<Trequest, Tresponse>(alias, inbox, request,
                    _response => { response = _response; ev.Set(); }))
                {
                    ev.WaitOne(Network.BaseSocket.MAX_REQUEST_TIME_MS);
                }
            }
            finally
            {
                _mrePool.Free(ev);
            }
            return response;
        }
    }
}
