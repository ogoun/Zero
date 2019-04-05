namespace ZeroLevel.Network
{/*
    public static class NetworkStats
    {
        #region Send info

        private static long _sended_bytes = 0;
        private static long _sended_kbytes = 0;
        private static long _sended_mbytes = 0;
        private static long _sended_gbytes = 0;
        private static SpinLock _s_lock = new SpinLock();
        private static void IncrementSended(long count)
        {
            bool taked = false;
            _s_lock.Enter(ref taked);
            _sended_bytes += count;
            if (_sended_bytes > 1024)
            {
                var kb = (_sended_bytes >> 10);
                _sended_bytes -= (kb << 10);
                _sended_kbytes += kb;
                if (_sended_kbytes > 1024)
                {
                    var mb = (_sended_kbytes >> 10);
                    _sended_kbytes -= (mb << 10);
                    _sended_mbytes += mb;
                    if (_sended_mbytes > 1024)
                    {
                        var gb = (_sended_mbytes >> 10);
                        _sended_mbytes -= (gb << 10);
                        _sended_gbytes += gb;
                    }
                }
            }
            if (taked)
            {
                _s_lock.Exit();
            }
        }

        public static string SendInfo()
        {
            if (_sended_gbytes > 0)
            {
                return $"{_sended_gbytes}Gb {_sended_mbytes}Mb {_sended_kbytes}Kb {_sended_bytes} bytes";
            }
            if (_sended_mbytes > 0)
            {
                return $"{_sended_mbytes}Mb {_sended_kbytes}Kb {_sended_bytes} bytes";
            }
            if (_sended_kbytes > 0)
            {
                return $"{_sended_kbytes}Kb {_sended_bytes} bytes";
            }
            return $"{_sended_bytes} bytes";
        }

        #endregion Send info

        #region Receive info

        private static long _received_bytes = 0;
        private static long _received_kbytes = 0;
        private static long _received_mbytes = 0;
        private static long _received_gbytes = 0;
        private static SpinLock _r_lock = new SpinLock();
        private static void IncrementReceived(long count)
        {
            bool taked = false;
            _r_lock.Enter(ref taked);
            _received_bytes += count;
            if (_received_bytes > 1024)
            {
                var kb = (_received_bytes >> 10);
                _received_bytes -= (kb << 10);
                _received_kbytes += kb;
                if (_received_kbytes > 1024)
                {
                    var mb = (_received_kbytes >> 10);
                    _received_kbytes -= (mb << 10);
                    _received_mbytes += mb;
                    if (_received_mbytes > 1024)
                    {
                        var gb = (_received_mbytes >> 10);
                        _received_mbytes -= (gb << 10);
                        _received_gbytes += gb;
                    }
                }
            }
            if (taked)
            {
                _r_lock.Exit();
            }
        }

        public static string ReceiveInfo()
        {
            if (_received_gbytes > 0)
            {
                return $"{_received_gbytes}Gb {_received_mbytes}Mb {_received_kbytes}Kb {_received_bytes} bytes";
            }
            if (_received_mbytes > 0)
            {
                return $"{_received_mbytes}Mb {_received_kbytes}Kb {_received_bytes} bytes";
            }
            if (_received_kbytes > 0)
            {
                return $"{_received_kbytes}Kb {_received_bytes} bytes";
            }
            return $"{_received_bytes} bytes";
        }

        #endregion Receive info

        #region RPS

        public static long RPS => _rps;

        private static long _rps_task = -1;
        private static long _rps = 0;
        private static long[] _rps_data = new long[8];
        public static void EnableCalculateRps()
        {
            if (_rps_task == -1)
            {
                _rps_task = Sheduller.RemindEvery(TimeSpan.FromSeconds(1), () =>
                {
                    for (int i = 0; i < _rps_data.Length - 1; i++)
                    {
                        _rps_data[i] = _rps_data[i + 1];
                    }
                    _rps_data[_rps_data.Length - 1] = _sendFrameCount + _receiveFrameCount;
                    _rps = ((_rps_data[_rps_data.Length - 1] - _rps_data[0]) >> 3);
                });
            }
        }
        public static void DisableCalculateRps()
        {
            if (_rps_task != -1)
            {
                Sheduller.Remove(_rps_task);
                _rps_task = -1;
            }
        }

        #endregion RPS

        public static long _sendFrameCount = 0;
        public static long _receiveFrameCount = 0;
        public static long _corruptedFrameCount = 0;

        public static long SendFrameCount { get { return _sendFrameCount; } }
        public static long ReceiveFrameCount { get { return _receiveFrameCount; } }
        public static long CorruptedFrameCount { get { return _corruptedFrameCount; } }

        public static void Send(byte[] data) { Interlocked.Increment(ref _sendFrameCount); IncrementSended(data.LongLength); }
        public static void Receive(byte[] data) { Interlocked.Increment(ref _receiveFrameCount); IncrementReceived(data.LongLength); }
        public static void Corrupted() => Interlocked.Increment(ref _corruptedFrameCount);
    }*/
}