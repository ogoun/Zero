using System.IO;
using System.IO.Compression;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Sleopok.Engine.Services
{
    public static class Compressor
    {
        private static void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];
            int cnt;
            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }

        public static byte[] Compress(string[] documents)
        {
            return Compress(MessageSerializer.SerializeCompatible(documents));
        }

        public static string[] DecompressToDocuments(byte[] data)
        {
            var bytes = Decompress(data);
            return MessageSerializer.DeserializeCompatible<string[]>(bytes);
        }

        public static byte[] Compress(byte[] data)
        {
            using (var msi = new MemoryStream(data))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    CopyTo(msi, gs);
                }
                return mso.ToArray();
            }
        }

        public static byte[] Decompress(byte[] data)
        {
            using (var msi = new MemoryStream(data))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    CopyTo(gs, mso);
                }
                return mso.ToArray();
            }
        }

    }
}
