using System;
using System.Net;
using ZeroLevel.Models;

namespace ZeroLevel.Network
{
    internal sealed class ExClient
        : ZBaseNetwork, IExClient, IZBackward
    {
        private readonly IZTransport _transport;
        private readonly ExRouter _router;
        private readonly FrameExchange _fe;

        public event Action Connected = () => { };

        public new ZTransportStatus Status => _transport.Status;

        public ExClient(IZTransport transport)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            transport.OnConnect += Transport_OnConnect;
            _fe = new FrameExchange(transport);
            _router = new ExRouter();
            transport.OnServerMessage += Transport_OnServerMessage;
        }

        public void ForceConnect()
        {
            try
            {
                _transport.EnsureConnection();
            }
            catch { }
        }

        private void Transport_OnConnect()
        {
            Connected();
        }

        private void Transport_OnServerMessage(object sender, Frame e)
        {
            try
            {
                _router.HandleMessage(e, this);
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[ExClient] Fault handle server message");
            }
            finally
            {
                e?.Release();
            }
        }

        public IPEndPoint Endpoint => _fe.Endpoint;

        public override void Dispose()
        {
            _fe.Dispose();
        }

        public void RegisterInbox<T>(string inbox, Action<T, long, IZBackward> handler)
        {
            _router.RegisterInbox(inbox, handler);
        }

        public void RegisterInbox<T>(Action<T, long, IZBackward> handler)
        {
            _router.RegisterInbox(DEFAULT_MESSAGE_INBOX, handler);
        }

        public InvokeResult Request<Tresp>(Action<Tresp> callback)
        {
            return _fe.Request<Tresp>(DEFAULT_REQUEST_INBOX, resp => callback(resp));
        }

        public InvokeResult Request<Tresp>(string inbox, Action<Tresp> callback)
        {
            return _fe.Request<Tresp>(inbox, resp => callback(resp));
        }

        public InvokeResult Request<Treq, Tresp>(Treq obj, Action<Tresp> callback)
        {
            return _fe.Request<Treq, Tresp>(DEFAULT_REQUEST_INBOX, obj, resp => callback(resp));
        }

        public InvokeResult Request<Treq, Tresp>(string inbox, Treq obj, Action<Tresp> callback)
        {
            return _fe.Request<Treq, Tresp>(inbox, obj, resp => callback(resp));
        }

        public InvokeResult Send<T>(T obj)
        {
            return _fe.Send<T>(DEFAULT_MESSAGE_INBOX, obj);
        }

        public InvokeResult Send<T>(string inbox, T obj)
        {
            return _fe.Send<T>(inbox, obj);
        }

        public InvokeResult SendBackward(Frame frame)
        {
            return _fe.Send(frame);
        }

        public InvokeResult SendBackward<T>(string inbox, T obj)
        {
            return Send(inbox, obj);
        }

        public InvokeResult Send()
        {
            return _fe.Send(DEFAULT_MESSAGE_INBOX);
        }

        public InvokeResult Send(string inbox)
        {
            return _fe.Send(inbox);
        }
    }
}