using System;
using System.Threading.Tasks;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.Network
{
    public sealed class FrameParser
    {
        #region private models
        private enum ParserState
        {
            WaitNew,
            WaitSize,
            Proceeding
        }

        private class _Accum
        {
            public byte[] Payload;
            public int Size;
            public bool SizeFilled;
            public bool PayloadFilled;
            public bool Corrupted;

            public void Reset()
            {
                Size = 0;
                offset = 0;
                Payload = null;
                SizeFilled = false;
                PayloadFilled = false;
                Corrupted = false;
            }

            private byte[] _size_buf = new byte[4];
            private int offset;

            public int WriteSize(byte[] buf, int start, int length)
            {
                for (; offset < 4 && start < length; offset++, start++)
                {
                    _size_buf[offset] = buf[start];
                }
                if (offset == 4)
                {
                    Size = BitConverter.ToInt32(_size_buf, 0);
                    SizeFilled = true;
                    offset = 0;
                    if (Size == 0)
                    {
                        // Как минимум 1 байт с контрольной суммой должен быть
                        Corrupted = true;
                    }
                }
                return start;
            }

            public int WritePayload(byte[] buf, int start, int length)
            {
                if (Payload == null)
                {
                    Payload = new byte[Size];
                    var mask = ((byte)(ZBaseNetwork.PACKET_HEADER_START_BYTE ^ _size_buf[0] ^ _size_buf[1] ^ _size_buf[2] ^ _size_buf[3]));
                    if (buf[start] != mask)
                    {
                        Corrupted = true;
                        return start;
                    }
                    start = start + 1;
                }
                int i = start;
                for (; offset < Size && i < length; offset++, i++)
                {
                    Payload[offset] = buf[i];
                }
                if (offset == Size)
                {
                    var mask = ((byte)(ZBaseNetwork.PACKET_HEADER_START_BYTE ^ _size_buf[0] ^ _size_buf[1] ^ _size_buf[2] ^ _size_buf[3]));
                    NetworkStreamFastObfuscator.DeHashData(Payload, mask);
                    PayloadFilled = true;
                }
                return i;
            }
        }
        #endregion

        private void FireOnFrame(byte[] payload)
        {
            Frame frame;
            try
            {
                frame = MessageSerializer.Deserialize<Frame>(payload);
            }
            catch (Exception ex)
            {
                //NetworkStats.Corrupted();
                Log.SystemError(ex, "[FrameParser] Fault deserialize frame from incomig data");
                return;
            }
            try
            {
                Task.Run(() => OnIncomingFrame?.Invoke(frame));
                //NetworkStats.Receive(payload);
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, "[FrameParser] Fault handle frame");
            }
        }

        public event Action<Frame> OnIncomingFrame;
        private readonly _Accum _accum = new _Accum();

        private ParserState _state = ParserState.WaitNew;
        private readonly object _push_lock = new object();
        /// <summary>
        /// Parse with state machine
        /// </summary>
        public void Push(byte[] part, int start, int length)
        {
            lock (_push_lock)
            {
                __Push(part, start, length);
            }
        }

        private void __Push(byte[] part, int start, int length)
        {
            if (part == null || length == 0 || start >= length) return;
            while (start < length)
            {
                switch (_state)
                {
                    case ParserState.WaitNew:
                        {
                            for (; start < length; start++)
                            {
                                // Поиск начала заголовка пакета
                                if ((part[start] & ZBaseNetwork.PACKET_HEADER_START_BYTE) == ZBaseNetwork.PACKET_HEADER_START_BYTE)
                                {
                                    _accum.Reset();
                                    _state = ParserState.WaitSize;
                                    start += 1;
                                    break;
                                }
                            }
                        }
                        break;
                    case ParserState.WaitSize:
                        {
                            start = _accum.WriteSize(part, start, length);
                            if (_accum.SizeFilled)
                            {
                                if (_accum.Corrupted || _accum.Size < 1 || _accum.Size > ZBaseNetwork.MAX_FRAME_PAYLOAD_SIZE)
                                {
                                    //NetworkStats.Corrupted();
                                    _state = ParserState.WaitNew;
                                }
                                else
                                {
                                    _state = ParserState.Proceeding;
                                }
                            }
                        }
                        break;
                    case ParserState.Proceeding:
                        {
                            start = _accum.WritePayload(part, start, length);
                            if (_accum.Corrupted)
                            {
                               // NetworkStats.Corrupted();
                                _state = ParserState.WaitNew;
                            }
                            else if (_accum.PayloadFilled)
                            {
                                FireOnFrame(_accum.Payload);
                                _state = ParserState.WaitNew;
                            }
                        }
                        break;
                }
            }
        }
    }
}
