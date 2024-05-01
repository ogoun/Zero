using System;
using System.IO;
using System.Security.Cryptography;

namespace ZeroLevel.Services.Encryption
{
    public class AesEncryptor
        : IDisposable
    {
        protected const int DEFAULT_STREAM_BUFFER_SIZE = 16384;
        private static readonly byte[] SALT = new byte[] { 0x26, 0xdc, 0xff, 0x00, 0xad, 0xed, 0x7a, 0xee, 0xc5, 0xfe, 0x07, 0xaf, 0x4d, 0x08, 0x22, 0x3c };

        #region Crypt fields

        private Aes _aes;
        private CryptoStream _stream;

        #endregion Crypt fields

        public AesEncryptor(Stream stream, string password, byte[] salt = null!)
        {
            _aes = Aes.Create();
            using (var pdb = new Rfc2898DeriveBytes(password, SALT))
            {
                _aes.Key = pdb.GetBytes(32);
                _aes.IV = pdb.GetBytes(16);
            }
            _stream = new CryptoStream(stream, _aes.CreateEncryptor(), CryptoStreamMode.Write);
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

        public static byte[] Encrypt(byte[] plain, string password, byte[] salt = null!)
        {
            using (var aes = Aes.Create())
            {
                using (var pdb = new Rfc2898DeriveBytes(password, salt ?? SALT))
                {
                    aes.Key = pdb.GetBytes(32);
                    aes.IV = pdb.GetBytes(16);
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            cryptoStream.Write(plain, 0, plain.Length);
                            cryptoStream.Close();
                            return memoryStream.ToArray();
                        }
                    }
                }
            }
        }

        public static byte[] Encrypt(Stream stream, string password, byte[] salt = null!)
        {
            using (var aes = Aes.Create())
            {
                using (var pdb = new Rfc2898DeriveBytes(password, salt ?? SALT))
                {
                    aes.Key = pdb.GetBytes(32);
                    aes.IV = pdb.GetBytes(16);
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            Transfer(stream, cryptoStream);
                            cryptoStream.Close();
                            return memoryStream.ToArray();
                        }
                    }
                }
            }
        }

        public static void Encrypt(Stream inputStream, Stream outputStream, string password, byte[] salt = null!)
        {
            using (var aes = Aes.Create())
            {
                using (var pdb = new Rfc2898DeriveBytes(password, salt ?? SALT))
                {
                    aes.Key = pdb.GetBytes(32);
                    aes.IV = pdb.GetBytes(16);
                    using (var cryptoStream = new CryptoStream(outputStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        Transfer(inputStream, cryptoStream);
                        cryptoStream.Close();
                    }
                }
            }
        }

        public static byte[] Decrypt(byte[] cipher, string password, byte[] salt = null!)
        {
            using (var aes = Aes.Create())
            {
                using (var pdb = new Rfc2898DeriveBytes(password, salt ?? SALT))
                {
                    aes.Key = pdb.GetBytes(32);
                    aes.IV = pdb.GetBytes(16);
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cryptoStream.Write(cipher, 0, cipher.Length);
                            cryptoStream.Close();
                            return memoryStream.ToArray();
                        }
                    }
                }
            }
        }

        public static byte[] Decrypt(Stream stream, string password, byte[] salt = null!)
        {
            using (var aes = Aes.Create())
            {
                using (var pdb = new Rfc2898DeriveBytes(password, salt ?? SALT))
                {
                    aes.Key = pdb.GetBytes(32);
                    aes.IV = pdb.GetBytes(16);
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            Transfer(stream, cryptoStream);
                            cryptoStream.Close();
                            return memoryStream.ToArray();
                        }
                    }
                }
            }
        }

        public static void Decrypt(Stream inputStream, Stream outputStream, string password, byte[] salt = null!)
        {
            using (var aes = Aes.Create())
            {
                using (var pdb = new Rfc2898DeriveBytes(password, salt ?? SALT))
                {
                    aes.Key = pdb.GetBytes(32);
                    aes.IV = pdb.GetBytes(16);
                    using (var cryptoStream = new CryptoStream(outputStream, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        Transfer(inputStream, cryptoStream);
                        cryptoStream.Close();
                    }
                }
            }
        }

        #endregion OneTime Read/Write

        public void Dispose()
        {
            try
            {
                _stream.Flush();
                _stream.Close();
                _stream.Dispose();
            }
            catch { }
            _aes.Clear();
            _aes.Dispose();
        }
    }
}