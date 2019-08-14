using System;
using System.Threading.Tasks;
using ZeroLevel.Services;

namespace ZeroLevel.Network
{
    public sealed class FrameParser
    {
        #region private models
        private enum ParserState
        {
            WaitNew,
            WaitSize,
            WaitIdentity,
            Proceeding
        }

        private class _Accum
        {
            public int Identity;
            public int Size;
            public byte[] Payload;
            public FrameType Type;

            public bool SizeFilled;
            public bool IdentityFilled;
            public bool PayloadFilled;
            public bool Corrupted;


            public void Reset(byte magic)
            {
                Identity = 0;
                Size = 0;
                offset = 0;
                Payload = null;
                SizeFilled = false;
                IdentityFilled = false;
                PayloadFilled = false;
                Corrupted = false;

                switch (magic)
                {
                    case NetworkPacketFactory.MAGIC: Type = FrameType.Message; break;
                    case NetworkPacketFactory.MAGIC_REQUEST: Type = FrameType.Request; break;
                    case NetworkPacketFactory.MAGIC_RESPONSE: Type = FrameType.Response; break;
                    case NetworkPacketFactory.MAGIC_KEEP_ALIVE: Type = FrameType.KeepAlive; break;
                }
            }

            private byte[] _size_buf = new byte[4];
            private byte[] _id_buf = new byte[4];
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
                        // At least 1 byte with checksum must be
                        Corrupted = true;
                    }
                }
                return start;
            }

            public int WriteIdentity(byte[] buf, int start, int length)
            {
                for (; offset < 4 && start < length; offset++, start++)
                {
                    _id_buf[offset] = buf[start];
                }
                if (offset == 4)
                {
                    Identity = BitConverter.ToInt32(_id_buf, 0);
                    IdentityFilled = true;
                    offset = 0;
                }
                return start;
            }

            public int WritePayload(byte[] buf, int start, int length)
            {
                if (Payload == null)
                {
                    Payload = new byte[Size];
                    var mask = ((byte)(NetworkPacketFactory.MAGIC ^ _size_buf[0] ^ _size_buf[1] ^ _size_buf[2] ^ _size_buf[3]));
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
                    var mask = ((byte)(NetworkPacketFactory.MAGIC ^ _size_buf[0] ^ _size_buf[1] ^ _size_buf[2] ^ _size_buf[3]));
                    DeHashData(Payload, mask);
                    PayloadFilled = true;
                }
                return i;
            }

            private static byte DeHashData(byte[] data, byte initialmask)
            {
                if (data.Length == 0) return 0;
                byte checksum = initialmask;
                for (var i = data.Length - 1; i > 0; i--)
                {
                    data[i] ^= data[i - 1];
                    checksum ^= data[i];
                }
                data[0] ^= initialmask;
                checksum ^= data[0];
                return checksum;
            }
        }

        #endregion private models

        public event Action<FrameType, int, byte[]> OnIncoming;

        private readonly _Accum _accum = new _Accum();
        private ParserState _state = ParserState.WaitNew;
        private readonly object _push_lock = new object();

        /// <summary>
        /// Parse with state machine
        /// </summary>
        public void Push(byte[] part, int length)
        {
            lock (_push_lock)
            {
                __Push(part, 0, length);
            }
        }

        private void FireOnFrame(FrameType type, int identity, byte[] payload)
        {
            try
            {
                OnIncoming?.Invoke(type, identity, payload);
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, "[FrameParser.FireOnFrame] Fault handle incoming data");
            }
        }

        private void __Push(byte[] part, int position, int length)
        {
            if (part == null || length == 0 || position >= length) return;
            while (position < length)
            {
                switch (_state)
                {
                    case ParserState.WaitNew:
                        {
                            for (; position < length; position++)
                            {
                                // Search for the beginning of the package header
                                if ((part[position] & NetworkPacketFactory.MAGIC) == NetworkPacketFactory.MAGIC)
                                {
                                    _accum.Reset(part[position]);
                                    _state = ParserState.WaitSize;
                                    position++;
                                    break;
                                }
                            }
                        }
                        break;

                    case ParserState.WaitSize:
                        {
                            position = _accum.WriteSize(part, position, length);
                            if (_accum.SizeFilled)
                            {
                                if (_accum.Corrupted || _accum.Size < 1 || _accum.Size > BaseSocket.MAX_FRAME_PAYLOAD_SIZE)
                                {
                                    _state = ParserState.WaitNew;
                                }
                                else
                                {
                                    switch (_accum.Type)
                                    {
                                        case FrameType.KeepAlive:
                                        case FrameType.Message:
                                            _state = ParserState.Proceeding;
                                            break;
                                        case FrameType.Request:
                                        case FrameType.Response:
                                            _state = ParserState.WaitIdentity;
                                            break;
                                    }
                                }
                            }
                        }
                        break;

                    case ParserState.WaitIdentity:
                        {
                            position = _accum.WriteIdentity(part, position, length);
                            if (_accum.IdentityFilled)
                            {
                                _state = ParserState.Proceeding;
                            }
                        }
                        break;

                    case ParserState.Proceeding:
                        {
                            position = _accum.WritePayload(part, position, length);
                            if (_accum.Corrupted)
                            {
                                _state = ParserState.WaitNew;
                            }
                            else if (_accum.PayloadFilled)
                            {
                                FireOnFrame(_accum.Type, _accum.Identity, _accum.Payload);
                                _state = ParserState.WaitNew;
                            }
                        }
                        break;
                }
            }
        }
    }
}