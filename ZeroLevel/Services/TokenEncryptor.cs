using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services
{
    public class TokenEncryptor
    {
        private sealed class Cryptor
        {
            private const int Rfc2898KeygenIterations = 100;
            private const int AesKeySizeInBits = 128;
            private readonly byte[] _password;
            private readonly byte[] _salt;
            public Cryptor(string pwd, string salt)
            {
                _password = Encoding.ASCII.GetBytes(pwd);
                _salt = Encoding.ASCII.GetBytes(salt);
            }

            public byte[] Encrypt(byte[] data)
            {
                using (Aes aes = new AesManaged())
                {
                    aes.Padding = PaddingMode.PKCS7;
                    aes.KeySize = AesKeySizeInBits;
                    int KeyStrengthInBytes = aes.KeySize / 8;
                    System.Security.Cryptography.Rfc2898DeriveBytes rfc2898 =
                        new System.Security.Cryptography.Rfc2898DeriveBytes(_password, _salt, Rfc2898KeygenIterations);
                    aes.Key = rfc2898.GetBytes(KeyStrengthInBytes);
                    aes.IV = rfc2898.GetBytes(KeyStrengthInBytes);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(data, 0, data.Length);
                        }
                        return ms.ToArray();
                    }
                }
            }

            public byte[] Decrypt(byte[] data)
            {
                using (Aes aes = new AesManaged())
                {
                    aes.Padding = PaddingMode.PKCS7;
                    aes.KeySize = AesKeySizeInBits;
                    int KeyStrengthInBytes = aes.KeySize / 8;
                    System.Security.Cryptography.Rfc2898DeriveBytes rfc2898 =
                        new System.Security.Cryptography.Rfc2898DeriveBytes(_password, _salt, Rfc2898KeygenIterations);
                    aes.Key = rfc2898.GetBytes(KeyStrengthInBytes);
                    aes.IV = rfc2898.GetBytes(KeyStrengthInBytes);

                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(data, 0, data.Length);
                        }
                        return ms.ToArray();
                    }
                }
            }
        }

        private Cryptor _cryptor;
        public TokenEncryptor(string key, string salt)
        {
            _cryptor = new Cryptor(key, salt);
        }

        public T ReadFromToken<T>(string token)
            where T : IBinarySerializable
        {
            if (string.IsNullOrWhiteSpace(token) == false && string.CompareOrdinal(token, "null") != 0)
            {
                var data = Convert.FromBase64String(token);
                var decryptedBytes = _cryptor.Decrypt(data);
                var decryptedValue = MessageSerializer.Deserialize<T>(decryptedBytes);
                return decryptedValue;
            }
            return default!;
        }

        public string WriteToToken<T>(T value)
            where T : IBinarySerializable
        {
            var decryptedBytes = MessageSerializer.Serialize<T>(value);
            var encryptedBytes = _cryptor.Encrypt(decryptedBytes);
            var token = Convert.ToBase64String(encryptedBytes);
            return token;
        }
    }
}
