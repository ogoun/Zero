using System.Collections.Generic;
using System.IO;

namespace ZeroLevel.Services.Storages.PartitionFileSystemStorage
{
    public interface IPartitionDataConverter<TRecord>
    {
        IEnumerable<TRecord> ReadFromStorage(Stream stream);
        void WriteToStorage(Stream stream, IEnumerable<TRecord> data);
    }
}
