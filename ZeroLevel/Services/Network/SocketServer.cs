using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ZeroLevel.Network.SDL;
using ZeroLevel.Services;

namespace ZeroLevel.Network
{
    internal sealed class SocketServer
        : BaseSocket, IRouter
    {
        private Socket _serverSocket;
        private ReaderWriterLockSlim _connection_set_lock = new ReaderWriterLockSlim();
        private Dictionary<IPEndPoint, ExClient> _connections = new Dictionary<IPEndPoint, ExClient>();

        private readonly IRouter _router;
        public IPEndPoint LocalEndpoint { get; }
        public event Action<ISocketClient> OnDisconnect = _ => { };
        public event Action<ExClient> OnConnect = _ => { };

        public IEnumerable<IPEndPoint> ConnectionList
        {
            get
            {
                try
                {
                    _connection_set_lock.EnterReadLock();
                    return _connections.Select(c => c.Value.EndPoint).ToList();
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
            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
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
                    _serverSocket.BeginAccept(BeginAcceptCallback, null);
                    _connection_set_lock.EnterWriteLock();

                    var connection = new SocketClient(client_socket, _router);
                    connection.OnDisconnect += Connection_OnDisconnect;
                    _connections[connection.Endpoint] = new ExClient(connection);
                    connection.UseKeepAlive(TimeSpan.FromMilliseconds(BaseSocket.MINIMUM_HEARTBEAT_UPDATE_PERIOD_MS));
                    ConnectEventRise(_connections[connection.Endpoint]);

                    Dbg.Timestamp((int)DbgNetworkEvents.ServerClientConnected, $"{connection.Endpoint.Address}:{connection.Endpoint.Port}");
                }
                catch (Exception ex)
                {
                    Broken();
                    Log.SystemError(ex, "[ZSocketServer] Error with connect accepting");
                }
                finally
                {
                    _connection_set_lock.ExitWriteLock();
                }
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

                Dbg.Timestamp((int)DbgNetworkEvents.ServerClientDisconnect, $"{client.Endpoint.Address}:{client.Endpoint.Port}");
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
        public void HandleRequest(Frame frame, ISocketClient client, Action<byte[]> handler) => _router.HandleRequest(frame, client, handler);
        public IServer RegisterInbox(string inbox, MessageHandler handler) => _router.RegisterInbox(inbox, handler);
        public IServer RegisterInbox(MessageHandler handler) => _router.RegisterInbox(handler);

        public IServer RegisterInbox<T>(string inbox, MessageHandler<T> handler) => _router.RegisterInbox<T>(inbox, handler);
        public IServer RegisterInbox<T>(MessageHandler<T> handler) => _router.RegisterInbox<T>(handler);
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
