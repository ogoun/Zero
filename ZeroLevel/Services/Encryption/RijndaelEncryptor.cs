using System;
using System.IO;
using System.Security.Cryptography;

namespace ZeroLevel.Services.Encryption
{
    public class RijndaelEncryptor
        : IDisposable
    {
        protected const int DEFAULT_STREAM_BUFFER_SIZE = 16384;
        private static readonly byte[] SALT = new byte[] { 0x26, 0xdc, 0xff, 0x00, 0xad, 0xed, 0x7a, 0xee, 0xc5, 0xfe, 0x07, 0xaf, 0x4d, 0x08, 0x22, 0x3c };

        #region Crypt fields
        private Rijndael _rijndael;
        private CryptoStream _stream;
        #endregion

        public RijndaelEncryptor(Stream stream, string password, byte[] salt = null)
        {
            _rijndael = Rijndael.Create();
            using (var pdb = new Rfc2898DeriveBytes(password, SALT))
            {
                _rijndael.Key = pdb.GetBytes(32);
                _rijndael.IV = pdb.GetBytes(16);
            }
            _stream = new CryptoStream(stream, _rijndael.CreateEncryptor(), CryptoStreamMode.Write);
        }

        public void Write(byte[] data)
        {
            _stream.Write(data, 0, data.Length);
        }

        #region OneTime Read/Write
        protected static void Transfer(Stream input, Stream output)
        {
            if (input.CanRead == false)
            {
                throw new InvalidOperationException("Input stream can not be read.");
            }
            if (output.CanWrite == false)
            {
                throw new InvalidOperationException("Output stream can not be write.");
            }
            var readed = 0;
            var buffer = new byte[DEFAULT_STREAM_BUFFER_SIZE];
            while ((readed = input.Read(buffer, 0, buffer.Length)) != 0)
            {
                output.Write(buffer, 0, readed);
            }
            output.Flush();
        }

        public static byte[] Encrypt(byte[] plain, string password, byte[] salt = null)
        {
            using (var rijndael = Rijndael.Create())
            {
                using (var pdb = new Rfc2898DeriveBytes(password, salt ?? SALT))
                {
                    rijndael.Key = pdb.GetBytes(32);
                    rijndael.IV = pdb.GetBytes(16);
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, rijndael.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            cryptoStream.Write(plain, 0, plain.Length);
                            cryptoStream.Close();
                            return memoryStream.ToArray();
                        }
                    }
                }
            }
        }

        public static byte[] Encrypt(Stream stream, string password, byte[] salt = null)
        {
            using (var rijndael = Rijndael.Create())
            {
                using (var pdb = new Rfc2898DeriveBytes(password, salt ?? SALT))
                {
                    rijndael.Key = pdb.GetBytes(32);
                    rijndael.IV = pdb.GetBytes(16);
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, rijndael.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            Transfer(stream, cryptoStream);
                            cryptoStream.Close();
                            return memoryStream.ToArray();
                        }
                    }
                }
            }
        }

        public static void Encrypt(Stream inputStream, Stream outputStream, string password, byte[] salt = null)
        {
            using (var rijndael = Rijndael.Create())
            {
                using (var pdb = new Rfc2898DeriveBytes(password, salt ?? SALT))
                {
                    rijndael.Key = pdb.GetBytes(32);
                    rijndael.IV = pdb.GetBytes(16);
                    using (var cryptoStream = new CryptoStream(outputStream, rijndael.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        Transfer(inputStream, cryptoStream);
                        cryptoStream.Close();
                    }
                }
            }
        }

        public static byte[] Decrypt(byte[] cipher, string password, byte[] salt = null)
        {
            using (var rijndael = Rijndael.Create())
            {
                using (var pdb = new Rfc2898DeriveBytes(password, salt ?? SALT))
                {
                    rijndael.Key = pdb.GetBytes(32);
                    rijndael.IV = pdb.GetBytes(16);
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, rijndael.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cryptoStream.Write(cipher, 0, cipher.Length);
                            cryptoStream.Close();
                            return memoryStream.ToArray();
                        }
                    }
                }
            }
        }

        public static byte[] Decrypt(Stream stream, string password, byte[] salt = null)
        {
            using (var rijndael = Rijndael.Create())
            {
                using (var pdb = new Rfc2898DeriveBytes(password, salt ?? SALT))
                {
                    rijndael.Key = pdb.GetBytes(32);
                    rijndael.IV = pdb.GetBytes(16);
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, rijndael.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            Transfer(stream, cryptoStream);
                            cryptoStream.Close();
                            return memoryStream.ToArray();
                        }
                    }
                }
            }
        }

        public static void Decrypt(Stream inputStream, Stream outputStream, string password, byte[] salt = null)
        {
            using (var rijndael = Rijndael.Create())
            {
                using (var pdb = new Rfc2898DeriveBytes(password, salt ?? SALT))
                {
                    rijndael.Key = pdb.GetBytes(32);
                    rijndael.IV = pdb.GetBytes(16);
                    using (var cryptoStream = new CryptoStream(outputStream, rijndael.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        Transfer(inputStream, cryptoStream);
                        cryptoStream.Close();
                    }
                }
            }
        }
        #endregion

        public void Dispose()
        {
            try
            {
                _stream.Flush();
                _stream.Close();
                _stream.Dispose();
            }
            catch { }
            _rijndael.Clear();
            _rijndael.Dispose();
        }
    }
}
