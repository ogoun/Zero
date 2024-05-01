using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ZeroLevel.Sleopok.Engine.Services.Indexes
{
    public interface IIndexBuilder<T>
        : IDisposable
    {
        Task Write(IEnumerable<T> batch);
        Task Complete();
    }
}
