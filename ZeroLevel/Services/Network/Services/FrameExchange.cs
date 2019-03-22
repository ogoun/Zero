using System;
using System.Net;
using ZeroLevel.Models;
using ZeroLevel.Services.Network.Contract;

namespace ZeroLevel.Services.Network.Services
{
    internal sealed class FrameExchange
        : IDisposable
    {
        private IZTransport _current;
        public IPEndPoint Endpoint => _current?.Endpoint;
        public bool IsConnected => _current?.Status == ZTransportStatus.Working;

        public FrameExchange(IZTransport transport)
        {
            _current = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public InvokeResult Send<T>(string inbox, T obj)
        {
            try
            {
                var frame = FrameBuilder.BuildFrame(obj, inbox);
                _current.Send(frame);
                return InvokeResult.Succeeding();
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, "[FrameExchange] Fault send frame");
                return InvokeResult.Fault(ex.Message);
            }
        }

        public InvokeResult Send(Frame frame)
        {
            try
            {
                _current.Send(frame);
                return InvokeResult.Succeeding();
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, "[FrameExchange] Fault send frame");
                return InvokeResult.Fault(ex.Message);
            }
        }

        public InvokeResult Request<Treq, Tresp>(string inbox, Treq obj, Action<Tresp> callback, Action<string> fault = null)
        {
            try
            {
                var frame = FrameBuilder.BuildRequestFrame(obj, inbox);
                _current.Request(frame, response_data =>
                {
                    callback(response_data.Read<Tresp>());
                }, fault);
                return InvokeResult.Succeeding();
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, "[FrameExchange] Fault send frame");
                return InvokeResult.Fault(ex.Message);
            }
        }

        public InvokeResult Request<Tresp>(string inbox, Action<Tresp> callback, Action<string> fault = null)
        {
            try
            {
                var frame = FrameBuilder.BuildRequestFrame(inbox);
                _current.Request(frame, response_data =>
                {
                    callback(response_data.Read<Tresp>());
                }, fault);
                return InvokeResult.Succeeding();
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, "[FrameExchange] Fault send frame");
                return InvokeResult.Fault(ex.Message);
            }
        }

        public void Dispose()
        {
            _current?.Dispose();
        }
    }
}
