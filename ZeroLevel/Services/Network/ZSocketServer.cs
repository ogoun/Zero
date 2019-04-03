using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ZeroLevel.Services.Network
{
    public abstract class ZSocketServer
        : ZBaseNetwork
    {
        public IPEndPoint LocalEndpoint { get { return _endpoint; } }

        public event Action<IZBackward> OnDisconnect = (c) => { };

        public event Action<IZBackward> OnConnect = (c) => { };

        public IEnumerable<IPEndPoint> ConnectionList
        {
            get
            {
                try
                {
                    _connection_set_lock.EnterReadLock();
                    return _connections.Select(c => c.Endpoint).ToList();
                }
                finally
                {
                    _connection_set_lock.ExitReadLock();
                }
            }
        }

        #region Private members

        private Socket _serverSocket;
        private IPEndPoint _endpoint;
        private ReaderWriterLockSlim _connection_set_lock = new ReaderWriterLockSlim();
        private HashSet<ZSocketServerClient> _connections = new HashSet<ZSocketServerClient>();
        private readonly Frame _pingFrame = FrameBuilder.BuildFrame(DEFAULT_PING_INBOX);
        private long _heartbeat_task = -1;

        private void DisconnectEventRise(IZBackward client)
        {
            try
            {
                OnDisconnect?.Invoke(client);
            }
            catch
            { }
        }

        private void ConnectEventRise(IZBackward client)
        {
            try
            {
                OnConnect?.Invoke(client);
            }
            catch
            { }
        }

        private void Heartbeat()
        {
            var enumerator = _connections.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    var connection = enumerator.Current;
                    if ((DateTime.UtcNow.Ticks - connection.LastNetworkActionTimestamp) >= HEARTBEAT_PING_PERIOD_TICKS)
                    {
                        connection.SendBackward(_pingFrame);
                    }
                }
            }
            catch { }
            GC.Collect(1, GCCollectionMode.Forced, false);
        }

        private void BeginAcceptCallback(IAsyncResult ar)
        {
            if (_status == ZTransportStatus.Working)
            {
                try
                {
                    var client_socket = _serverSocket.EndAccept(ar);
                    _serverSocket.BeginAccept(BeginAcceptCallback, null);
                    _connection_set_lock.EnterWriteLock();
                    var connection = new ZSocketServerClient(client_socket, Handle, HandleRequest);
                    connection.OnConnectionBroken += Connection_OnConnectionBroken;
                    _connections.Add(connection);
                    ConnectEventRise(connection);
                }
                catch (Exception ex)
                {
                    _status = ZTransportStatus.Broken;
                    Log.SystemError(ex, $"[ZSocketServer] Error with connect accepting");
                }
                finally
                {
                    _connection_set_lock.ExitWriteLock();
                }
            }
        }

        private void Connection_OnConnectionBroken(ZSocketServerClient connection)
        {
            connection.OnConnectionBroken -= Connection_OnConnectionBroken;
            try
            {
                _connection_set_lock.EnterWriteLock();
                _connections.Remove(connection);
            }
            finally
            {
                _connection_set_lock.ExitWriteLock();
            }
            connection.Dispose();
        }

        #endregion Private members

        public ZSocketServer(IPEndPoint endpoint)
        {
            _endpoint = endpoint;
            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            _serverSocket.Bind(endpoint);
            _serverSocket.Listen(100);
            _heartbeat_task = Sheduller.RemindEvery(TimeSpan.FromMilliseconds(HEARTBEAT_UPDATE_PERIOD_MS), Heartbeat);
            _status = ZTransportStatus.Working;
            _serverSocket.BeginAccept(BeginAcceptCallback, null);
        }

        protected abstract void Handle(Frame frame, IZBackward client);

        protected abstract Frame HandleRequest(Frame frame, IZBackward client);

        public override void Dispose()
        {
            if (_status == ZTransportStatus.Disposed)
            {
                return;
            }
            Sheduller.Remove(_heartbeat_task);
            _status = ZTransportStatus.Disposed;
            _serverSocket.Close();
            _serverSocket.Dispose();
            try
            {
                _connection_set_lock.EnterReadLock();
                foreach (var c in _connections)
                {
                    c.Dispose();
                }
            }
            finally
            {
                _connection_set_lock.ExitReadLock();
            }
            _connection_set_lock.Dispose();
        }
    }
}