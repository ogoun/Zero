using System;

namespace ZeroLevel.Services.Network.Models
{
    internal sealed class RequestInfo
    {
        private Action<Frame> _handler;
        private Action<string> _failHandler;
        private long _timestamp;
        public long Timestamp { get { return _timestamp; } }
        private bool _sended;
        public bool Sended { get { return _sended; } }

        public void Reset(Action<Frame> handler, Action<string> failHandler)
        {
            _sended = false;
            _handler = handler;
            _failHandler = failHandler;
        }

        public void StartSend()
        {
            _sended = true;
            _timestamp = DateTime.UtcNow.Ticks;
        }

        public void Success(Frame frame)
        {
            _handler(frame);
            frame?.Release();
        }

        public void Fail(string reasonPhrase)
        {
            _failHandler(reasonPhrase);
        }
    }
}