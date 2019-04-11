using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ZeroLevel.Services.Pools;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Network
{
    public class ZSocketClient
        : ZBaseNetwork, IZTransport
    {
        private class RequestBuffer
        {
            private readonly object _reqeust_lock = new object();
            private Dictionary<long, RequestInfo> _requests = new Dictionary<long, RequestInfo>();
            private static ObjectPool<RequestInfo> _ri_pool = new ObjectPool<RequestInfo>(() => new RequestInfo());

            public void RegisterForFrame(Frame frame, Action<Frame> callback, Action<string> fail = null)
            {
                var ri = _ri_pool.Allocate();
                lock (_reqeust_lock)
                {
                    ri.Reset(callback, fail);
                    _requests.Add(frame.FrameId, ri);
                }
            }

            public void Fail(long frameId, string message)
            {
                RequestInfo ri = null;
                lock (_reqeust_lock)
                {
                    if (_requests.ContainsKey(frameId))
                    {
                        ri = _requests[frameId];
                        _requests.Remove(frameId);
                    }
                }
                if (ri != null)
                {
                    ri.Fail(message);
                    _ri_pool.Free(ri);
                }
            }

            public void Success(long frameId, Frame frame)
            {
                RequestInfo ri = null;
                lock (_reqeust_lock)
                {
                    if (_requests.ContainsKey(frameId))
                    {
                        ri = _requests[frameId];
                        _requests.Remove(frameId);
                    }
                }
                if (ri != null)
                {
                    ri.Success(frame);
                    _ri_pool.Free(ri);
                }
            }

            public void StartSend(long frameId)
            {
                RequestInfo ri = null;
                lock (_reqeust_lock)
                {
                    if (_requests.ContainsKey(frameId))
                    {
                        ri = _requests[frameId];
                    }
                }
                if (ri != null)
                {
                    ri.StartSend();
                }
            }

            public void TestForTimeouts()
            {
                var now_ticks = DateTime.UtcNow.Ticks;
                var to_remove = new List<long>();
                lock (_reqeust_lock)
                {
                    foreach (var pair in _requests)
                    {
                        if (pair.Value.Sended == false) continue;
                        var diff = now_ticks - pair.Value.Timestamp;
                        if (diff > ZBaseNetwork.MAX_REQUEST_TIME_TICKS)
                        {
                            to_remove.Add(pair.Key);
                        }
                    }
                }
                foreach (var key in to_remove)
                {
                    Fail(key, "Timeout");
                }
            }
        }

        #region Private

        private Socket _clientSocket;
        private NetworkStream _stream;
        private FrameParser _parser = new FrameParser();
        private Thread _sendThread;
        private long _heartbeat_key;
        private long _last_rw_time = DateTime.UtcNow.Ticks;
        private readonly byte[] _buffer = new byte[DEFAULT_RECEIVE_BUFFER_SIZE];
        private readonly object _reconnection_lock = new object();

        private readonly BlockingCollection<Frame> _send_queue = new BlockingCollection<Frame>();

        private readonly RequestBuffer _requests = new RequestBuffer();

        #endregion Private

        public event EventHandler<Frame> OnServerMessage = (_, __) => { };

        public event Action OnConnect = () => { };

        public event Action OnDisconnect = () => { };

        public IPEndPoint Endpoint { get; }
        public bool IsEmptySendQueue { get { return _send_queue.Count == 0; } }

        public ZSocketClient(IPEndPoint ep)
        {
            Endpoint = ep;
            _parser.OnIncomingFrame += _parser_OnIncomingFrame;

            _heartbeat_key = Sheduller.RemindEvery(TimeSpan.FromMilliseconds(HEARTBEAT_UPDATE_PERIOD_MS), Heartbeat);

            _sendThread = new Thread(SendFramesJob);
            _sendThread.IsBackground = true;
            _sendThread.Start();
        }

        #region Private methods

        private void Heartbeat()
        {
            try
            {
                EnsureConnection();
            }
            catch
            {
                Broken();
                return;
            }
            _requests.TestForTimeouts();
            try
            {
                Request(FrameBuilder.BuildFrame(DEFAULT_PING_INBOX), r => { });
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, "Fault ping reauest");
            }
            var diff_request_ms = ((DateTime.UtcNow.Ticks - _last_rw_time) / TimeSpan.TicksPerMillisecond);
            if (diff_request_ms > (HEARTBEAT_UPDATE_PERIOD_MS * 2))
            {
                var port = (_clientSocket.LocalEndPoint as IPEndPoint)?.Port;
                Log.Debug($"[ZClient] server disconnected, because last data was more thas {diff_request_ms} ms ago. Client port {port}");
                Broken();
            }
        }

        private void _parser_OnIncomingFrame(Frame frame)
        {
            if (frame == null || frame.Inbox == null) return;
            _last_rw_time = DateTime.UtcNow.Ticks;
            if (frame.IsRequest)
            {
                // Got response on request with id = packet_id
                try
                {
                    _requests.Success(frame.FrameId, frame);
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, $"[ZClient] Fault handle response");
                }
            }
            else
            {
                // Got server comand
                if (frame.Inbox.Equals(DEFAULT_PING_INBOX, StringComparison.Ordinal))
                {
                    _last_rw_time = DateTime.UtcNow.Ticks;
                }
                else
                {
                    try
                    {
                        OnServerMessage?.Invoke(this, frame);
                    }
                    catch (Exception ex)
                    {
                        Log.SystemError(ex, $"[ZClient] Fault handle server message");
                    }
                }
            }
            frame?.Release();
        }

        private void ReceiveAsyncCallback(IAsyncResult ar)
        {
            try
            {
                EnsureConnection();
                var count = _stream.EndRead(ar);
                if (count > 0)
                {
                    _parser.Push(_buffer, 0, count);
                    _last_rw_time = DateTime.UtcNow.Ticks;
                }
                if (Status == ZTransportStatus.Working)
                {
                    _stream.BeginRead(_buffer, 0, DEFAULT_RECEIVE_BUFFER_SIZE, ReceiveAsyncCallback, null);
                }
            }
            catch (ObjectDisposedException)
            {
                /// Nothing
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[ZSocketServerClient] Error read data");
                Broken();
                OnDisconnect();
            }
        }

        private void SendFramesJob()
        {
            Frame frame = null;
            while (Status != ZTransportStatus.Disposed)
            {
                if (_send_queue.IsCompleted)
                {
                    return;
                }
                if (Status != ZTransportStatus.Working)
                {
                    Thread.Sleep(100);
                    try
                    {
                        EnsureConnection();                        
                    }
                    catch (Exception ex)
                    {
                        Log.SystemError(ex, "[ZSocketClient] Send next frame fault");
                    }
                    if (Status == ZTransportStatus.Disposed) return;
                    continue;
                }
                try
                {
                    frame = _send_queue.Take();
                    var data = NetworkStreamFastObfuscator.PrepareData(MessageSerializer.Serialize(frame));
                    if (data != null && data.Length > 0)
                    {
                        if (frame.IsRequest)
                        {
                            _requests.StartSend(frame.FrameId);
                        }
                        _stream.Write(data, 0, data.Length);
                        _last_rw_time = DateTime.UtcNow.Ticks;
                        //NetworkStats.Send(data);
                    }
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, $"[ZSocketServerClient] Backward send error.");
                    Broken();
                    OnDisconnect();
                }
                finally
                {
                    frame?.Release();
                }
            }
        }

        #endregion Private methods

        #region API

        private bool TryConnect()
        {
            if (Status == ZTransportStatus.Working)
            {
                return true;
            }
            if (Status == ZTransportStatus.Disposed)
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
                Log.SystemError(ex, $"[ZSocketClient] Connection fault");
                Broken();
                return false;
            }
            Working();
            OnConnect();
            return true;
        }

        public void EnsureConnection()
        {
            lock (_reconnection_lock)
            {
                if (Status == ZTransportStatus.Disposed)
                {
                    throw new ObjectDisposedException("connection");
                }
                if (Status != ZTransportStatus.Working)
                {
                    if (false == TryConnect())
                    {
                        throw new ObjectDisposedException("No connection");
                    }
                }
            }
        }

        public void Send(Frame frame)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            EnsureConnection();
            if (frame != null && false == _send_queue.IsAddingCompleted)
            {
                while (_send_queue.Count >= ZBaseNetwork.MAX_SEND_QUEUE_SIZE)
                {
                    Thread.Sleep(50);
                }
                _send_queue.Add(frame);
            }
        }

        public void Request(Frame frame, Action<Frame> callback, Action<string> fail = null)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            try
            {
                EnsureConnection();
            }
            catch (Exception ex)
            {
                fail?.Invoke(ex.Message);
                return;
            }
            _requests.RegisterForFrame(frame, callback, fail);
            try
            {
                Send(frame);
            }
            catch (Exception ex)
            {
                fail?.Invoke(ex.Message);
                Broken();
                OnDisconnect();
                Log.SystemError(ex, $"[ZSocketClient] Request error. Frame '{frame.FrameId}'. Inbox '{frame.Inbox}'");
            }
        }

        #endregion API

        #region Helper

        private static Socket MakeClientSocket()
        {
            var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            s.SetIPProtectionLevel(IPProtectionLevel.Unrestricted);
            s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
            return s;
        }

        #endregion Helper

        public override void Dispose()
        {
            if (Status == ZTransportStatus.Working)
            {
                OnDisconnect();
            }
            Disposed();
            Sheduller.Remove(_heartbeat_key);
            _stream?.Close();
            _stream?.Dispose();
        }
    }
}