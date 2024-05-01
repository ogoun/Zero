using MemoryPools;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Network
{
    public class SocketClient
        : BaseSocket, ISocketClient
    {
        #region Private

        #region Queues
        private struct IncomingFrame
        {
            public FrameType type;
            public int identity;
            public byte[] data;
        }
        private struct OutcomingFrame
        {
            public bool is_request;
            public int identity;
            public byte[] data;
        }

        private readonly JetValPool<OutcomingFrame> _outcomingFramesPool = new JetValPool<OutcomingFrame>();
        private ConcurrentQueue<IncomingFrame> _incoming_queue = new ConcurrentQueue<IncomingFrame>();
        private ConcurrentQueue<OutcomingFrame> _outcoming_queue = new ConcurrentQueue<OutcomingFrame>();
        private ManualResetEventSlim _outcomingFrameEvent = new ManualResetEventSlim(false);
        #endregion

        private Socket _clientSocket;
        private FrameParser _parser;
        private readonly RequestBuffer _requests = new RequestBuffer();
        private readonly byte[] _buffer = new byte[DEFAULT_RECEIVE_BUFFER_SIZE];
        private bool _socket_freezed = false; // используется для связи сервер-клиент, запрещает пересоздание сокета
        private readonly object _reconnection_lock = new object();
        private long _heartbeat_key;
        private Thread _receiveThread;
        private Thread _sendingThread;

        #endregion Private

        public IRouter Router { get; }

        public SocketClient(IPEndPoint ep, IRouter router)
        {
            try
            {
                _clientSocket = new Socket(ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
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
            _receiveThread = new Thread(IncomingFramesJob);
            _receiveThread.IsBackground = true;
            _receiveThread.Start();

            _sendingThread = new Thread(OutcomingFramesJob);
            _sendingThread.IsBackground = true;
            _sendingThread.Start();


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

        public void Request(Frame frame, Action<byte[]> callback, Action<string> fail = null!)
        {
            if (Status != SocketClientStatus.Working) throw new Exception($"[SocketClient.Request] Socket status: {Status}");
            var data = NetworkPacketFactory.Reqeust(MessageSerializer.Serialize(frame), out int id);
            _requests.RegisterForFrame(id, callback, fail);
            Send(id, true, data);
        }

        public void Send(Frame frame)
        {
            if (Status != SocketClientStatus.Working) throw new Exception($"[SocketClient.Send] Socket status: {Status}");
            var data = NetworkPacketFactory.Message(MessageSerializer.Serialize(frame));
            Send(0, false, data);
        }

        public void Response(byte[] data, int identity)
        {
            if (data == null!) throw new ArgumentNullException(nameof(data));
            if (Status != SocketClientStatus.Working) throw new Exception($"[SocketClient.Response] Socket status: {Status}");
            Send(0, false, NetworkPacketFactory.Response(data, identity));
        }
        #endregion

        #region Private methods      

        private void _parser_OnIncoming(FrameType type, int identity, byte[] data)
        {
            try
            {
                if (type == FrameType.KeepAlive) return;
                _incoming_queue.Enqueue(new IncomingFrame
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

            Send(0, false, NetworkPacketFactory.KeepAliveMessage());
        }

        private void IncomingFramesJob()
        {
            IncomingFrame frame = default(IncomingFrame);
            while (Status != SocketClientStatus.Disposed)
            {
                if (_incoming_queue.TryDequeue(out frame))
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
                                        if (response != null!)
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
                else
                {
                    Thread.Sleep(100);
                }
            }
        }

        private void OutcomingFramesJob()
        {
            while (Status != SocketClientStatus.Disposed)
            {
                if (Status == SocketClientStatus.Working)
                {
                    if (_outcomingFrameEvent.Wait(100))
                    {
                        _outcomingFrameEvent.Reset();
                    }
                    while (_outcoming_queue.TryDequeue(out var frame))
                    {
                        try
                        {
                            if (frame.is_request)
                            {
                                _requests.StartSend(frame.identity);
                            }
                            _clientSocket.Send(frame.data, frame.data.Length, SocketFlags.None);
                            //var sended = _clientSocket.Send(frame.data, frame.data.Length, SocketFlags.None);
                            //return sended == frame.data.Length;
                        }
                        catch (Exception ex)
                        {
                            Log.SystemError(ex, $"[SocketClient.OutcomingFramesJob] _str_clientSocketeam.Send");
                            Broken();
                            OnDisconnect(this);
                        }
                        finally
                        {
                            _outcomingFramesPool.Return(frame);
                        }
                    }                    
                }
                else
                {
                    Thread.Sleep(400);
                }
            }
        }

        private void Send(int id, bool is_request, byte[] data)
        {
            var frame = _outcomingFramesPool.Get();
            frame.data = data;
            frame.identity = id;
            frame.is_request = is_request;
            _outcoming_queue.Enqueue(frame);
            _outcomingFrameEvent.Set();
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
