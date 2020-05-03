using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ZeroLevel.Services.Pools;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Network
{
    public class SocketClient
        : BaseSocket, ISocketClient
    {
        #region Private

        #region Queues
        private class IncomingFrame
        {
            private IncomingFrame() { }
            public FrameType type;
            public int identity;
            public byte[] data;

            public static IncomingFrame NewFrame() => new IncomingFrame();
        }

        private class SendFrame
        {
            private SendFrame() { }

            public bool isRequest;
            public int identity;
            public byte[] data;

            public static SendFrame NewFrame() => new SendFrame();
        }

        private ObjectPool<IncomingFrame> _incoming_frames_pool = new ObjectPool<IncomingFrame>(() => IncomingFrame.NewFrame());
        private ObjectPool<SendFrame> _send_frames_pool = new ObjectPool<SendFrame>(() => SendFrame.NewFrame());

        private BlockingCollection<IncomingFrame> _incoming_queue = new BlockingCollection<IncomingFrame>();
        private BlockingCollection<SendFrame> _send_queue = new BlockingCollection<SendFrame>(BaseSocket.MAX_SEND_QUEUE_SIZE);
        #endregion

        private Socket _clientSocket;
        private FrameParser _parser;
        private readonly RequestBuffer _requests = new RequestBuffer();
        private readonly byte[] _buffer = new byte[DEFAULT_RECEIVE_BUFFER_SIZE];
        private bool _socket_freezed = false; // используется для связи сервер-клиент, запрещает пересоздание сокета
        private readonly object _reconnection_lock = new object();
        private long _heartbeat_key;
        private Thread _sendThread;
        private Thread _receiveThread;

        #endregion Private

        public IRouter Router { get; }

        public bool IsEmptySendQueue { get { return _send_queue.Count == 0; } }

        public SocketClient(IPEndPoint ep, IRouter router)
        {
            try
            {
                _clientSocket = new Socket(ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _clientSocket.SetIPProtectionLevel(IPProtectionLevel.Unrestricted);
                _clientSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
                _clientSocket.Connect(ep);
                OnConnect(this);
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[SocketClient.ctor] connection fault. Endpoint: {ep.Address}:{ep.Port}");
                Broken();
                return;
            }
            Router = router;
            Endpoint = ep;
            _parser = new FrameParser(_parser_OnIncoming);

            Working();

            StartInternalThreads();
            StartReceive();
        }

        public SocketClient(Socket socket, IRouter router)
        {
            Router = router;
            _clientSocket = socket;
            Endpoint = (IPEndPoint)_clientSocket.RemoteEndPoint;
            _parser = new FrameParser(_parser_OnIncoming);
            _socket_freezed = true;

            Working();

            StartInternalThreads();
            StartReceive();
        }

        private void StartInternalThreads()
        {
            _sendThread = new Thread(SendFramesJob);
            _sendThread.IsBackground = true;
            _sendThread.Start();

            _receiveThread = new Thread(IncomingFramesJob);
            _receiveThread.IsBackground = true;
            _receiveThread.Start();

            _heartbeat_key = Sheduller.RemindEvery(TimeSpan.FromMilliseconds(MINIMUM_HEARTBEAT_UPDATE_PERIOD_MS), Heartbeat);
        }

        private void StartReceive()
        {
            try
            {
                _clientSocket.BeginReceive(_buffer, 0, DEFAULT_RECEIVE_BUFFER_SIZE, SocketFlags.None, ReceiveAsyncCallback, null);
            }
            catch (NullReferenceException)
            {
                Broken();
                Log.SystemError("[SocketClient.TryConnect] Client : Null Reference Exception - On Connect (begin receive section)");
                _clientSocket.Disconnect(false);
            }
            catch (SocketException e)
            {
                Broken();
                Log.SystemError(e, "[SocketClient.TryConnect] Client : Exception - On Connect (begin receive section)");
                _clientSocket.Disconnect(false);
            }
        }

        #region API
        public event Action<ISocketClient> OnConnect = (_) => { };
        public event Action<ISocketClient> OnDisconnect = (_) => { };
        public IPEndPoint Endpoint { get; }

        public void Request(Frame frame, Action<byte[]> callback, Action<string> fail = null)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            if (Status != SocketClientStatus.Working) throw new Exception($"[SocketClient.Request] Socket status: {Status}");
            var data = NetworkPacketFactory.Reqeust(MessageSerializer.Serialize(frame), out int id);
            frame.Release();

            if (!_send_queue.IsAddingCompleted)
            {
                while (_send_queue.Count >= MAX_SEND_QUEUE_SIZE)
                {
                    Thread.Sleep(1);
                }
                _requests.RegisterForFrame(id, callback, fail);
                var sf = _send_frames_pool.Allocate();
                sf.isRequest = true;
                sf.identity = id;
                sf.data = data;
                _send_queue.Add(sf);
            }
        }

        public void Send(Frame frame)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            if (Status != SocketClientStatus.Working) throw new Exception($"[SocketClient.Send] Socket status: {Status}");
            var data = NetworkPacketFactory.Message(MessageSerializer.Serialize(frame));
            frame.Release();

            if (!_send_queue.IsAddingCompleted)
            {
                while (_send_queue.Count >= MAX_SEND_QUEUE_SIZE)
                {
                    Thread.Sleep(1);
                }
                var sf = _send_frames_pool.Allocate();
                sf.isRequest = false;
                sf.identity = 0;
                sf.data = data;
                _send_queue.Add(sf);
            }
        }

        public void Response(byte[] data, int identity)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (Status != SocketClientStatus.Working) throw new Exception($"[SocketClient.Response] Socket status: {Status}");
            if (!_send_queue.IsAddingCompleted)
            {
                while (_send_queue.Count >= MAX_SEND_QUEUE_SIZE)
                {
                    Thread.Sleep(1);
                }
                var sf = _send_frames_pool.Allocate();
                sf.isRequest = false;
                sf.identity = 0;
                sf.data = NetworkPacketFactory.Response(data, identity);
                _send_queue.Add(sf);
            }
        }
        #endregion

        #region Private methods      

        private void _parser_OnIncoming(FrameType type, int identity, byte[] data)
        {
            try
            {
                if (type == FrameType.KeepAlive) return;
                var inc_frame = _incoming_frames_pool.Allocate();
                inc_frame.data = data;
                inc_frame.type = type;
                inc_frame.identity = identity;
                _incoming_queue.Add(inc_frame);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[SocketClient._parser_OnIncoming]");
            }
        }

        public void ReceiveAsyncCallback(IAsyncResult ar)
        {
            try
            {
                var count = _clientSocket.EndReceive(ar);
                if (count > 0)
                {
                    _parser.Push(_buffer, count);
                }
                else
                {
                    // TODO or not TODO
                    Thread.Sleep(1);
                }
                StartReceive();
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

        private void EnsureConnection()
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
                    throw new Exception("No connection");
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
            var info = _send_frames_pool.Allocate();
            info.isRequest = false;
            info.identity = 0;
            info.data = NetworkPacketFactory.KeepAliveMessage();
            _send_queue.Add(info);
        }

        private void IncomingFramesJob()
        {
            IncomingFrame frame = default(IncomingFrame);
            while (Status != SocketClientStatus.Disposed && !_send_queue.IsCompleted)
            {
                try
                {
                    if (_incoming_queue.TryTake(out frame, 100))
                    {
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
                        finally
                        {
                            _incoming_frames_pool.Free(frame);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, "[SocketClient.IncomingFramesJob] _incoming_queue.Take");
                    if (Status != SocketClientStatus.Disposed)
                    {
                        _incoming_queue.Dispose();
                        _incoming_queue = new BlockingCollection<IncomingFrame>();
                    }
                    if (frame != null)
                    {
                        _incoming_frames_pool.Free(frame);
                    }
                    continue;
                }
            }
        }

        private void SendFramesJob()
        {
            SendFrame frame = null;
            while (Status != SocketClientStatus.Disposed && !_send_queue.IsCompleted)
            {
                try
                {
                    if (_send_queue.TryTake(out frame, 100))
                    {
                        try
                        {
                            if (frame.isRequest)
                            {
                                _requests.StartSend(frame.identity);
                            }
                            _clientSocket.Send(frame.data, frame.data.Length, SocketFlags.None);
                        }
                        catch (Exception ex)
                        {
                            Log.SystemError(ex, $"[SocketClient.SendFramesJob] _str_clientSocketeam.Send");
                            Broken();
                            OnDisconnect(this);
                        }
                        finally
                        {
                            _send_frames_pool.Free(frame);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, "[SocketClient.SendFramesJob] send_queue.TryTake");
                    if (Status != SocketClientStatus.Disposed)
                    {
                        _send_queue.Dispose();
                        _send_queue = new BlockingCollection<SendFrame>();
                    }
                    if (frame != null)
                    {
                        _send_frames_pool.Free(frame);
                    }
                    continue;
                }
            }
        }

        #endregion

        public override void Dispose()
        {
            if (Status == SocketClientStatus.Working)
            {
                OnDisconnect(this);
            }
            Disposed();
            Sheduller.Remove(_heartbeat_key);
            try
            {
                _clientSocket?.Close();
                _clientSocket?.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[SocketClient.Dispose]");
            }
        }
    }
}
