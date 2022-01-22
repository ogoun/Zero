using System;

namespace ZeroLevel.HNSW.Services
{
    public class Quantizator
    {
        private readonly float _min;
        private readonly float _max;
        private readonly float _diff;

        public Quantizator(float min, float max)
        {
            _min = min;
            _max = max;
            _diff = _max - _min;
        }

        public byte[] Quantize(float[] v)
        {
            var result = new byte[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                result[i] = _quantizeInRange(v[i]);
            }
            return result;
        }

        public int[] QuantizeToInt(float[] v)
        {
            var diff = v.Length % 4;
            int count = (v.Length - diff) / 4;
            var result = new int[((diff == 0) ? 0 : 1) + (v.Length / 4)];
            byte[] buf = new byte[4];
            int i = 0;
            for (; i < count * 4; i += 4)
            {
                buf[0] = _quantizeInRange(v[i]);
                buf[1] = _quantizeInRange(v[i + 1]);
                buf[2] = _quantizeInRange(v[i + 2]);
                buf[3] = _quantizeInRange(v[i + 3]);
                result[(i >> 2)] = BitConverter.ToInt32(buf);
            }
            if (diff != 0)
            {
                for (var j = 0; j < diff; j++)
                {
                    buf[j] = _quantizeInRange(v[i + j]);
                }
                for (var j = diff; j < 4; j++)
                {
                    buf[j] = 0;
                }
                result[(i >> 2)] = BitConverter.ToInt32(buf);
            }
            return result;
        }

        public long[] QuantizeToLong(float[] v)
        {
            var diff = v.Length % 8;
            int count = (v.Length - diff) / 8;
            var result = new long[((diff == 0) ? 0 : 1) + (v.Length / 8)];
            byte[] buf = new byte[8];
            int i = 0;
            for (; i < count * 8; i += 8)
            {
                buf[0] = _quantizeInRange(v[i + 0]);
                buf[1] = _quantizeInRange(v[i + 1]);
                buf[2] = _quantizeInRange(v[i + 2]);
                buf[3] = _quantizeInRange(v[i + 3]);
                buf[4] = _quantizeInRange(v[i + 4]);
                buf[5] = _quantizeInRange(v[i + 5]);
                buf[6] = _quantizeInRange(v[i + 6]);
                buf[7] = _quantizeInRange(v[i + 7]);

                result[(i >> 3)] = BitConverter.ToInt64(buf);
            }
            if (diff != 0)
            {
                for (var j = 0; j < diff; j++)
                {
                    buf[j] = _quantizeInRange(v[i + j]);
                }
                for (var j = diff; j < 8; j++)
                {
                    buf[j] = 0;
                }
                result[(i >> 3)] = BitConverter.ToInt64(buf);
            }
            return result;
        }

        //Map x in [0,1] to {0, 1, ..., 255}
        private byte _quantize(float x)
        {
            x = (int)Math.Floor(256 * x);
            if (x < 0) return 0;
            else if (x > 255) return 255;
            else return (byte)x;
        }

        //Map x in [min,max] to {0, 1, ..., 255}
        private byte _quantizeInRange(float x)
        {
            return _quantize((x - _min) / (_diff));
        }
    }
}
