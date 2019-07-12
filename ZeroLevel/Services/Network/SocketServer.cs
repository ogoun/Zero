using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ZeroLevel.Network
{
    public class SocketServer
        : BaseSocket
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

        public IRouter Router { get { return _router; } }

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
    }
}
