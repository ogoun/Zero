using System.Linq;
using System.Text;
using System.Threading;

namespace ZeroLevel.Services.Encryption
{
    public class FastObfuscator
    {
        private readonly byte[] _salt;
        private int _index = -1;

        private byte GetInitial() => (_salt == null || _salt.Length == 0) ? (byte)177 : _salt[Interlocked.Increment(ref _index) % _salt.Length];

        public FastObfuscator(string key)
        {
            _salt = Encoding.UTF8.GetBytes(key).Where(b => b > 0).ToArray();
        }

        public void HashData(byte[] data)
        {
            if (data.Length == 0) return;
            int i = 1;
            data[0] ^= GetInitial();
            for (; i < (data.Length - 8); i += 8)
            {
                data[i + 0] ^= data[i - 1];
                data[i + 1] ^= data[i + 0];
                data[i + 2] ^= data[i + 1];
                data[i + 3] ^= data[i + 2];
                data[i + 4] ^= data[i + 3];
                data[i + 5] ^= data[i + 4];
                data[i + 6] ^= data[i + 5];
                data[i + 7] ^= data[i + 6];
            }
            for (; i < data.Length; i++)
            {
                data[i] ^= data[i - 1];
            }
        }

        public void DeHashData(byte[] data)
        {
            int i;
            for (i = data.Length - 1; i > 9; i -= 8)
            {
                data[i - 0] ^= data[i - 1];
                data[i - 1] ^= data[i - 2];
                data[i - 2] ^= data[i - 3];
                data[i - 3] ^= data[i - 4];
                data[i - 4] ^= data[i - 5];
                data[i - 5] ^= data[i - 6];
                data[i - 6] ^= data[i - 7];
                data[i - 7] ^= data[i - 8];
            }
            for (; i >= 1; i--)
            {
                data[i] ^= data[i - 1];
            }
            data[0] ^= GetInitial();
        }
    }
}