using System;

namespace ZeroLevel.Network
{
    internal sealed class RequestInfo
    {
        private Action<byte[]> _handler;
        private Action<string> _failHandler;
        private long _timestamp;
        public long Timestamp { get { return _timestamp; } }
        private bool _sended;
        public bool Sended { get { return _sended; } }

        public void Reset(Action<byte[]> handler, Action<string> failHandler)
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

        public void Success(byte[] data)
        {
            _handler(data);
        }

        public void Fail(string reasonPhrase)
        {
            _failHandler(reasonPhrase);
        }
    }
}
