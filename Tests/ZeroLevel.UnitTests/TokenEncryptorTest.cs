using System;
using Xunit;
using ZeroLevel.Services;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.UnitTests
{
    public class TokenEncryptorTest
    {
        public class TestClaims
            : IBinarySerializable, IEquatable<TestClaims>
        {
            public string Login;
            public string Name;

            public void Deserialize(IBinaryReader reader)
            {
                this.Login = reader.ReadString();
                this.Name = reader.ReadString();
            }

            public override bool Equals(object obj)
            {
                return this.Equals(obj as TestClaims);
            }

            public bool Equals(TestClaims other)
            {
                if (other == null) return false;
                if (string.Compare(this.Login, other.Login, StringComparison.OrdinalIgnoreCase) != 0) return false;
                if (string.Compare(this.Name, other.Name, StringComparison.OrdinalIgnoreCase) != 0) return false;
                return true;
            }

            public void Serialize(IBinaryWriter writer)
            {
                writer.WriteString(this.Login);
                writer.WriteString(this.Name);
            }

            public override int GetHashCode()
            {
                return (Login?.GetHashCode() ?? 0) ^ (Name?.GetHashCode() ?? 0);
            }
        }

        [Fact]
        public void TokenEncryptorEncryptDecryptTest()
        {
            // Arrange
            var a1 = new TestClaims { Login = null, Name = "name" };
            var a2 = new TestClaims { Login = "login", Name = "name" };
            var a3 = new TestClaims { Login = null, Name = null };
            var key = "testkey";
            var salt = "testsalt";
            var tokenEncryptor = new TokenEncryptor(key, salt);

            // Act
            var a1_token = tokenEncryptor.WriteToToken(a1);
            var a2_token = tokenEncryptor.WriteToToken(a2);
            var a3_token = tokenEncryptor.WriteToToken(a3);

            var a1_decrypted = tokenEncryptor.ReadFromToken<TestClaims>(a1_token);
            var a2_decrypted = tokenEncryptor.ReadFromToken<TestClaims>(a2_token);
            var a3_decrypted = tokenEncryptor.ReadFromToken<TestClaims>(a3_token);

            // Assert
            Assert.True(a1.Equals(a1_decrypted));
            Assert.True(a2.Equals(a2_decrypted));
            Assert.True(a3.Equals(a3_decrypted));
        }
    }
}
