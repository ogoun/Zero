using System;
using System.Net;
using ZeroLevel.Services.Network.Contract;
using ZeroLevel.Services.Network.Services;

namespace ZeroLevel.Services.Network
{
    internal sealed class ExService
        : ZBaseNetwork, IExService
    {
        private readonly ExRouter _router;
        private readonly IZObservableServer _server;

        public event Action<IZBackward> OnConnect = c => { };

        public event Action<IZBackward> OnDisconnect = c => { };

        public ExService(IZObservableServer server)
        {
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _router = new ExRouter();
            _server.OnMessage += _server_OnMessage;
            _server.OnRequest += _server_OnRequest;
            _server.OnConnect += _server_OnConnect;
            _server.OnDisconnect += _server_OnDisconnect;
        }

        private void _server_OnDisconnect(IZBackward client)
        {
            this.OnDisconnect(client);
        }

        private void _server_OnConnect(IZBackward client)
        {
            this.OnConnect(client);
        }

        private Frame _server_OnRequest(Frame frame, IZBackward client)
        {
            return _router.HandleRequest(frame, client);
        }

        private void _server_OnMessage(Frame frame, IZBackward client)
        {
            _router.HandleMessage(frame, client);
        }

        public IPEndPoint Endpoint => _server.Endpoint;

        /// <summary>
        /// Registering an Inbox Handler
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="inbox">Inbox name</param>
        /// <param name="handler">Handler</param>
        public void RegisterInbox<T>(string inbox, Action<T, long, IZBackward> handler)
        {
            _router.RegisterInbox(inbox, handler);
        }

        public void RegisterInbox<T>(Action<T, long, IZBackward> handler)
        {
            _router.RegisterInbox(DEFAULT_MESSAGE_INBOX, handler);
        }

        /// <summary>
        /// Registration method responding to an incoming request
        /// </summary>
        /// <typeparam name="Treq">Type of input message</typeparam>
        /// <typeparam name="Tresp">Type of response</typeparam>
        /// <param name="protocol">Protocol</param>
        /// <param name="inbox">Inbox name</param>
        /// <param name="replier">Handler</param>
        public void RegisterInbox<Treq, Tresp>(string inbox, Func<Treq, long, IZBackward, Tresp> handler)
        {
            _router.RegisterInbox<Treq, Tresp>(inbox, handler);
        }

        public void RegisterInbox<Treq, Tresp>(Func<Treq, long, IZBackward, Tresp> handler)
        {
            _router.RegisterInbox<Treq, Tresp>(DEFAULT_REQUEST_INBOX, handler);
        }

        /// <summary>
        /// Registration of the method of responding to the incoming request, not receiving incoming data
        /// </summary>
        /// <typeparam name="Tresp">Type of response</typeparam>
        /// <param name="protocol">Protocol</param>
        /// <param name="inbox">Inbox name</param>
        /// <param name="replier">Handler</param>
        public void RegisterInbox<Tresp>(string inbox, Func<long, IZBackward, Tresp> handler)
        {
            _router.RegisterInbox<Tresp>(inbox, handler);
        }

        public void RegisterInbox<Tresp>(Func<long, IZBackward, Tresp> handler)
        {
            _router.RegisterInbox<Tresp>(DEFAULT_REQUEST_INBOX, handler);
        }

        public override void Dispose()
        {
            _server.Dispose();
        }
    }
}