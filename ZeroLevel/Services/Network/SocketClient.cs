using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using ZeroLevel.Services;
using ZeroLevel.Services.Pools;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Network
{
    public class SocketClient
        : BaseSocket, ISocketClient
    {
        #region Private
        private struct IncomingFrame
        {
            public FrameType type;
            public int identity;
            public byte[] data;
        }
        private struct SendFrame
        {
            public bool isRequest;
            public int identity;
            public byte[] data;
        }

        private Socket _clientSocket;
        private NetworkStream _stream;

        private FrameParser _parser = new FrameParser();
        private readonly RequestBuffer _requests = new RequestBuffer();

        private bool _socket_freezed = false; // используется для связи сервер-клиент, запрещает пересоздание сокета
        private int _current_heartbeat_period_in_ms = 0;
        private long _heartbeat_key = -1;
        private long _last_rw_time = DateTime.UtcNow.Ticks;
        private readonly byte[] _buffer = new byte[DEFAULT_RECEIVE_BUFFER_SIZE];
        private readonly object _reconnection_lock = new object();

        private Thread _sendThread;
        private Thread _receiveThread;
        private BlockingCollection<IncomingFrame> _incoming_queue = new BlockingCollection<IncomingFrame>();
        private BlockingCollection<SendFrame> _send_queue = new BlockingCollection<SendFrame>(BaseSocket.MAX_SEND_QUEUE_SIZE);
        #endregion Private

        public IRouter Router { get; }

        public bool IsEmptySendQueue { get { return _send_queue.Count == 0; } }

        public SocketClient(IPEndPoint ep, IRouter router)
        {
            Router = router;
            Endpoint = ep;
            _parser.OnIncoming += _parser_OnIncoming;
            StartInternalThreads();
            EnsureConnection();
        }

        public SocketClient(Socket socket, IRouter router)
        {
            Router = router;
            _socket_freezed = true;
            _clientSocket = socket;
            _stream = new NetworkStream(_clientSocket, true);
            Endpoint = (IPEndPoint)_clientSocket.RemoteEndPoint;
            _parser.OnIncoming += _parser_OnIncoming;
            StartInternalThreads();
            Working();

            _stream.BeginRead(_buffer, 0, DEFAULT_RECEIVE_BUFFER_SIZE, ReceiveAsyncCallback, null);
        }

        private void StartInternalThreads()
        {
            _sendThread = new Thread(SendFramesJob);
            _sendThread.IsBackground = true;
            _sendThread.Start();

            _receiveThread = new Thread(IncomingFramesJob);
            _receiveThread.IsBackground = true;
            _receiveThread.Start();
        }

        #region API
        public event Action<ISocketClient> OnConnect = (_) => { };
        public event Action<ISocketClient> OnDisconnect = (_) => { };
        public IPEndPoint Endpoint { get; }

        public void Request(Frame frame, Action<byte[]> callback, Action<string> fail = null)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            var data = NetworkPacketFactory.Reqeust(MessageSerializer.Serialize(frame), out int id);
            frame.Release();

            if (!_send_queue.IsAddingCompleted)
            {
                while (_send_queue.Count >= MAX_SEND_QUEUE_SIZE)
                {
                    Thread.Sleep(1);
                }
                _requests.RegisterForFrame(id, callback, fail);
                _send_queue.Add(new SendFrame
                {
                    isRequest = true,
                    data = data,
                    identity = id
                });

            }
        }

        public void ForceConnect()
        {
            EnsureConnection();
        }

        public void Send(Frame frame)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            var data = NetworkPacketFactory.Message(MessageSerializer.Serialize(frame));
            frame.Release();

            if (!_send_queue.IsAddingCompleted)
            {
                while (_send_queue.Count >= MAX_SEND_QUEUE_SIZE)
                {
                    Thread.Sleep(1);
                }
                _send_queue.Add(new SendFrame
                {
                    isRequest = false,
                    identity = 0,
                    data = data
                });
            }
        }
        
        public void Response(byte[] data, int identity)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (!_send_queue.IsAddingCompleted)
            {
                while (_send_queue.Count >= MAX_SEND_QUEUE_SIZE)
                {
                    Thread.Sleep(1);
                }
                _send_queue.Add(new SendFrame
                {
                    isRequest = false,
                    identity = 0,
                    data = NetworkPacketFactory.Response(data, identity)
                });
            }
        }

        public void UseKeepAlive(TimeSpan period)
        {
            if (_heartbeat_key != -1)
            {
                Sheduller.Remove(_heartbeat_key);
            }
            if (period != TimeSpan.Zero && period.TotalMilliseconds > MINIMUM_HEARTBEAT_UPDATE_PERIOD_MS)
            {
                _current_heartbeat_period_in_ms = (int)period.TotalMilliseconds;
                _heartbeat_key = Sheduller.RemindEvery(period, Heartbeat);
            }
            else
            {
                _current_heartbeat_period_in_ms = 0;
            }
        }
        #endregion

        #region Private methods      

        private void _parser_OnIncoming(FrameType type, int identity, byte[] data)
        {
            try
            {
                if (type == FrameType.KeepAlive) return;
                _incoming_queue.Add(new IncomingFrame
                {
                    data = data,
                    type = type,
                    identity = identity
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[SocketClient._parser_OnIncoming]");
            }
        }

        private bool TryConnect()
        {
            if (Status == SocketClientStatus.Working)
            {
                return true;
            }
            if (Status == SocketClientStatus.Disposed)
            {
                return false;
            }
            if (_clientSocket != null)
            {
                try
                {
                    _stream?.Close();
                    _stream?.Dispose();
                    _clientSocket.Dispose();
                }
                catch
                {
                    /* ignore */
                }
                _clientSocket = null;
                _stream = null;
            }
            try
            {
                _clientSocket = MakeClientSocket();
                _clientSocket.Connect(Endpoint);
                _stream = new NetworkStream(_clientSocket, true);
                _stream.BeginRead(_buffer, 0, DEFAULT_RECEIVE_BUFFER_SIZE, ReceiveAsyncCallback, null);
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, "[SocketClient.TryConnect] Connection fault");
                Broken();
                return false;
            }
            Working();
            OnConnect(this);
            return true;
        }

        public void EnsureConnection()
        {
            if (_socket_freezed)
            {
                return;
            }
            lock (_reconnection_lock)
            {
                if (Status == SocketClientStatus.Disposed)
                {
                    throw new ObjectDisposedException("connection");
                }
                if (Status != SocketClientStatus.Working)
                {
                    if (false == TryConnect())
                    {
                        throw new Exception("No connection");
                    }
                }
            }
        }

        private void Heartbeat()
        {
            try
            {
                EnsureConnection();
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, "[SocketClient.Heartbeat.EnsureConnection]");
                Broken();
                OnDisconnect(this);
                return;
            }
            _requests.TestForTimeouts();
            try
            {
                var info = new SendFrame
                {
                    isRequest = false,
                    identity = 0,
                    data = NetworkPacketFactory.KeepAliveMessage()
                };
                _send_queue.Add(info);
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, "[SocketClient.Heartbeat.Request]");
            }
            var diff_request_ms = ((DateTime.UtcNow.Ticks - _last_rw_time) / TimeSpan.TicksPerMillisecond);
            if (diff_request_ms > (_current_heartbeat_period_in_ms * 2))
            {
                var port = (_clientSocket.LocalEndPoint as IPEndPoint)?.Port;
                Log.Debug($"[SocketClient.Heartbeat] server disconnected, because last data was more thas {diff_request_ms} ms ago. Client port {port}");
                Broken();
            }
        }

        private void ReceiveAsyncCallback(IAsyncResult ar)
        {
            try
            {
                var count = _stream.EndRead(ar);
                if (count > 0)
                {
                    _parser.Push(_buffer, count);
                    _last_rw_time = DateTime.UtcNow.Ticks;
                }
                else
                {
                    // TODO or not TODO
                    Thread.Sleep(1);
                }
                EnsureConnection();
                _stream.BeginRead(_buffer, 0, DEFAULT_RECEIVE_BUFFER_SIZE, ReceiveAsyncCallback, null);
            }
            catch (ObjectDisposedException)
            {
                /// Nothing
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[SocketClient.ReceiveAsyncCallback] Error read data");
                Broken();
                OnDisconnect(this);
            }
        }

        private void IncomingFramesJob()
        {
            IncomingFrame frame = default(IncomingFrame);
            while (Status != SocketClientStatus.Disposed && !_send_queue.IsCompleted)
            {
                try
                {
                    frame = _incoming_queue.Take();
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, "[SocketClient.IncomingFramesJob] _incoming_queue.Take");
                    if (Status != SocketClientStatus.Disposed)
                    {
                        _incoming_queue.Dispose();
                        _incoming_queue = new BlockingCollection<IncomingFrame>();
                    }
                    continue;
                }
                try
                {
                    switch (frame.type)
                    {
                        case FrameType.Message:
                            Router?.HandleMessage(MessageSerializer.Deserialize<Frame>(frame.data), this);
                            break;
                        case FrameType.Request:
                            {
                                Router?.HandleRequest(MessageSerializer.Deserialize<Frame>(frame.data), this, frame.identity, (id, response) =>
                                {
                                    if (response != null)
                                    {
                                        this.Response(response, id);
                                    }
                                });
                            }
                            break;
                        case FrameType.Response:
                            {
                                _requests.Success(frame.identity, frame.data);
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, "[SocketClient.IncomingFramesJob] Handle frame");
                }
            }
        }

        private void SendFramesJob()
        {
            SendFrame frame;
            int unsuccess = 0;
            while (Status != SocketClientStatus.Disposed && !_send_queue.IsCompleted)
            {
                try
                {
                    frame = _send_queue.Take();
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, "[SocketClient.SendFramesJob] send_queue.Take");
                    if (Status != SocketClientStatus.Disposed)
                    {
                        _send_queue.Dispose();
                        _send_queue = new BlockingCollection<SendFrame>();
                    }
                    continue;
                }
                while (_stream?.CanWrite == false || Status != SocketClientStatus.Working)
                {
                    try
                    {
                        EnsureConnection();
                    }
                    catch (Exception ex)
                    {
                        Log.SystemError(ex, "[SocketClient.SendFramesJob] Connection broken");
                    }
                    if (Status == SocketClientStatus.Disposed)
                    {
                        return;
                    }
                    if (Status == SocketClientStatus.Broken)
                    {
                        unsuccess++;
                        if (unsuccess > 30) unsuccess = 30;
                    }
                    if (Status == SocketClientStatus.Working)
                    {
                        unsuccess = 0;
                        break;
                    }
                    Thread.Sleep(unsuccess * 128);
                }
                try
                {
                    if (frame.isRequest)
                    {
                        _requests.StartSend(frame.identity);
                    }
                    _stream.Write(frame.data, 0, frame.data.Length);
                    _last_rw_time = DateTime.UtcNow.Ticks;
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, $"[SocketClient.SendFramesJob] _stream.Write");
                    Broken();
                    OnDisconnect(this);
                }
            }
        }

        #endregion

        #region Helper

        private static Socket MakeClientSocket()
        {
            var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            s.SetIPProtectionLevel(IPProtectionLevel.Unrestricted);
            s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
            return s;
        }
        /* TODO to test
        public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            try
            {
                // Workaround for: https://github.com/dotnet/corefx/issues/24430
                using (cancellationToken.Register(Dispose))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return 0;
                    }

                    return await _stream.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (IOException exception)
            {
                if (exception.InnerException is SocketException socketException)
                {
                    ExceptionDispatchInfo.Capture(socketException).Throw();
                }

                throw;
            }
        }
        */
        #endregion Helper

        public override void Dispose()
        {
            if (Status == SocketClientStatus.Working)
            {
                OnDisconnect(this);
            }
            Disposed();
            Sheduller.Remove(_heartbeat_key);
            _stream?.Close();
            _stream?.Dispose();
        }
    }
}
