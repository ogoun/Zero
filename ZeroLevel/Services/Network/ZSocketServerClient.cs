using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Network
{
    internal sealed class ZSocketServerClient
        : ZBaseNetwork, IZBackward, IEquatable<ZSocketServerClient>
    {
        public IPEndPoint Endpoint { get; private set; }
        private readonly FrameParser _parser;
        private readonly Socket _socket;
        private NetworkStream _stream;
        private readonly Action<Frame, IZBackward> _handler;
        private readonly Func<Frame, IZBackward, Frame> _requestor;
        private readonly BlockingCollection<byte[]> _send_queue = new BlockingCollection<byte[]>();
        private Thread _sendThread;
        private readonly byte[] _buffer = new byte[DEFAULT_RECEIVE_BUFFER_SIZE];
        private long _last_rw_time = DateTime.UtcNow.Ticks;
        internal long LastNetworkActionTimestamp => _last_rw_time;

        public event Action<ZSocketServerClient> OnConnectionBroken = (c) => { };

        private void RizeConnectionBrokenEvent()
        {
            try { OnConnectionBroken?.Invoke(this); } catch { }
        }

        public ZSocketServerClient(Socket socket,
            Action<Frame, IZBackward> handler,
            Func<Frame, IZBackward, Frame> requestor)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _requestor = requestor ?? throw new ArgumentNullException(nameof(requestor));

            Endpoint = _socket.RemoteEndPoint as IPEndPoint;
            _stream = new NetworkStream(_socket, true);
            _parser = new FrameParser();
            _parser.OnIncomingFrame += _parser_OnIncomingFrame;

            Working();

            _sendThread = new Thread(SendFramesJob);
            _sendThread.IsBackground = true;
            _sendThread.Start();

            _stream.BeginRead(_buffer, 0, DEFAULT_RECEIVE_BUFFER_SIZE, ReceiveAsyncCallback, null);
        }

        public void SendBackward(Frame frame)
        {
            if (frame != null && Status == ZTransportStatus.Working && false == _send_queue.IsCompleted && false == _send_queue.IsAddingCompleted)
            {
                var data = MessageSerializer.Serialize(frame);
                try
                {
                    _send_queue.Add(NetworkStreamFastObfuscator.PrepareData(data));
                }
                catch (ObjectDisposedException)
                {
                    // Ignore
                }
                finally
                {
                    frame?.Release();
                }
            }
        }

        private void SendFramesJob()
        {
            byte[] data;
            while (Status == ZTransportStatus.Working)
            {
                if (_send_queue.IsCompleted)
                {
                    return;
                }
                try
                {
                    data = _send_queue.Take();
                    if (data != null && data.Length > 0)
                    {
                        _stream.Write(data, 0, data.Length);
                        _stream.Flush();
                        //Thread.Sleep(1);
                        _last_rw_time = DateTime.UtcNow.Ticks;
                        //NetworkStats.Send(data);
                    }
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, $"[ZSocketServerClient] Backward send error.");
                    Broken();
                    RizeConnectionBrokenEvent();
                }
            }
        }

        private void ReceiveAsyncCallback(IAsyncResult ar)
        {
            try
            {
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
                RizeConnectionBrokenEvent();
            }
        }

        private void _parser_OnIncomingFrame(Frame frame)
        {
            if (frame == null || frame.Inbox == null) return;
            if (frame.Inbox.Equals(DEFAULT_PING_INBOX, StringComparison.Ordinal))
            {
                SendBackward(frame);
            }
            else if (frame.IsRequest)
            {
                Frame response;
                try
                {
                    response = _requestor?.Invoke(frame, this);
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, $"[ZSocketServerClient] Fault make response for request '{frame.FrameId}' to inbox '{frame.Inbox}'");
                    response = FrameBuilder.BuildResponseFrame<string>(ex.Message, frame, DEFAULT_REQUEST_ERROR_INBOX);
                }
                finally
                {
                    frame?.Release();
                }
                if (response != null)
                {
                    SendBackward(response);
                }
            }
            else
            {
                try
                {
                    _handler?.Invoke(frame, this);
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, $"[ZSocketServerClient] Fault handle message '{frame.FrameId}' in inbox '{frame.Inbox}'");
                }
                finally
                {
                    frame?.Release();
                }
            }
        }

        public override void Dispose()
        {
            if (Status == ZTransportStatus.Disposed)
            {
                return;
            }
            Disposed();

            _send_queue.CompleteAdding();
            _send_queue.Dispose();

            this._stream.Flush();
            this._stream.Close();
            this._stream.Dispose();
        }

        public override int GetHashCode()
        {
            return Endpoint.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as ZSocketServerClient);
        }

        public bool Equals(ZSocketServerClient other)
        {
            if (other == null) return false;
            return this.Endpoint.Compare(other.Endpoint) == 0;
        }
    }
}