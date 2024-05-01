using System;
using System.Threading.Tasks;

namespace ZeroLevel.Sleopok.Engine.Services.Storage
{
    public interface IPartitionDataWriter
         : IDisposable
    {
        Task Write(string token, string document);
        Task Complete();
        long GetTotalRecords();
    }
}
