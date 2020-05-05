using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ZeroLevel.Network.SDL;

namespace ZeroLevel.Network
{
    public sealed class SocketServer
        : BaseSocket, IRouter
    {
        private Socket _serverSocket;
        private ReaderWriterLockSlim _connection_set_lock = new ReaderWriterLockSlim();
        private Dictionary<IPEndPoint, ExClient> _connections = new Dictionary<IPEndPoint, ExClient>();

        private readonly IRouter _router;
        public IPEndPoint LocalEndpoint { get; }
        public event Action<ISocketClient> OnDisconnect = _ => { };
        public event Action<IClient> OnConnect = _ => { };

        public IEnumerable<IPEndPoint> ConnectionList
        {
            get
            {
                try
                {
                    _connection_set_lock.EnterReadLock();
                    return _connections.Select(c => c.Value.Endpoint).ToList();
                }
                finally
                {
                    _connection_set_lock.ExitReadLock();
                }
            }
        }

        private void DisconnectEventRise(ISocketClient client)
        {
            try
            {
                OnDisconnect?.Invoke(client);
            }
            catch
            { }
        }

        private void ConnectEventRise(ExClient client)
        {
            try
            {
                OnConnect?.Invoke(client);
            }
            catch
            { }
        }

        public SocketServer(IPEndPoint endpoint, IRouter router)
        {
            _router = router;
            LocalEndpoint = endpoint;
            _serverSocket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            _serverSocket.Bind(endpoint);
            _serverSocket.Listen(100);
            Working();
            _serverSocket.BeginAccept(BeginAcceptCallback, null);
        }

        private void BeginAcceptCallback(IAsyncResult ar)
        {
            if (Status == SocketClientStatus.Working)
            {
                try
                {
                    var client_socket = _serverSocket.EndAccept(ar);
                    _connection_set_lock.EnterWriteLock();
                    var connection = new SocketClient(client_socket, _router);
                    connection.OnDisconnect += Connection_OnDisconnect;
                    _connections[connection.Endpoint] = new ExClient(connection);
                    ConnectEventRise(_connections[connection.Endpoint]);
                }
                catch (Exception ex)
                {
                    Broken();
                    Log.SystemError(ex, "[ZSocketServer.BeginAcceptCallback] Error with connect accepting");
                }
                finally
                {
                    _connection_set_lock.ExitWriteLock();                    
                }
                try
                {
                    _serverSocket.BeginAccept(BeginAcceptCallback, null);
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, "[ZSocketServer.BeginAcceptCallback] BeginAccept error");
                }
            }
            else
            {
                Log.Warning($"[ZSocketServer.BeginAcceptCallback] Server socket change state to: {Status}");
            }
        }

        private void Connection_OnDisconnect(ISocketClient client)
        {
            client.OnDisconnect -= Connection_OnDisconnect;
            try
            {
                _connection_set_lock.EnterWriteLock();
                _connections[client.Endpoint].Dispose();
                _connections.Remove(client.Endpoint);
            }
            finally
            {
                _connection_set_lock.ExitWriteLock();
            }
            DisconnectEventRise(client);
        }

        public override void Dispose()
        {
            try
            {
                foreach (var c in _connections)
                {
                    c.Value.Dispose();
                }
                _connections.Clear();
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, "[SocketServer.Dispose]");
            }
        }

        #region IRouter
        public void HandleMessage(Frame frame, ISocketClient client) => _router.HandleMessage(frame, client);
        public void HandleRequest(Frame frame, ISocketClient client, int identity, Action<int, byte[]> handler) => _router.HandleRequest(frame, client, identity, handler);
        public IServer RegisterInbox(string inbox, MessageHandler handler) => _router.RegisterInbox(inbox, handler);
        public IServer RegisterInbox(MessageHandler handler) => _router.RegisterInbox(handler);
        public IServer RegisterInbox<T>(string inbox, MessageHandler<T> handler) => _router.RegisterInbox<T>(inbox, handler);
        public IServer RegisterInbox<T>(MessageHandler<T> handler) => _router.RegisterInbox<T>(handler);

        public IServer RegisterInboxIfNoExists(string inbox, MessageHandler handler) => _router.RegisterInboxIfNoExists(inbox, handler);
        public IServer RegisterInboxIfNoExists(MessageHandler handler) => _router.RegisterInboxIfNoExists(handler);
        public IServer RegisterInboxIfNoExists<T>(string inbox, MessageHandler<T> handler) => _router.RegisterInboxIfNoExists<T>(inbox, handler);
        public IServer RegisterInboxIfNoExists<T>(MessageHandler<T> handler) => _router.RegisterInboxIfNoExists<T>(handler);

        public IServer RegisterInbox<Tresponse>(string inbox, RequestHandler<Tresponse> handler) => _router.RegisterInbox<Tresponse>(inbox, handler);
        public IServer RegisterInbox<Trequest, Tresponse>(string inbox, RequestHandler<Trequest, Tresponse> handler) => _router.RegisterInbox<Trequest, Tresponse>(inbox, handler);
        public IServer RegisterInbox<Tresponse>(RequestHandler<Tresponse> handler) => _router.RegisterInbox<Tresponse>(handler);
        public IServer RegisterInbox<Trequest, Tresponse>(RequestHandler<Trequest, Tresponse> handler) => _router.RegisterInbox<Trequest, Tresponse>(handler);

        public bool ContainsInbox(string inbox) => _router.ContainsInbox(inbox);
        public bool ContainsHandlerInbox(string inbox) => _router.ContainsHandlerInbox(inbox);
        public bool ContainsRequestorInbox(string inbox) => _router.ContainsRequestorInbox(inbox);
        public IEnumerable<InboxServiceDescription> CollectInboxInfo() => _router.CollectInboxInfo();
        #endregion
    }
}
