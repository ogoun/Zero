using System;

namespace ZeroLevel.Services.HashFunctions
{
    public interface IHash
    {
        uint Hash(string s);
        uint Hash(byte[] data);
        uint Hash(byte[] data, int offset, uint len, uint seed);
    }
}
