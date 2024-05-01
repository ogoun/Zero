using System.Collections.Generic;
using System.Threading.Tasks;

namespace ZeroLevel.Sleopok.Engine.Services.Indexes
{
    public interface IIndexReader<T>
    {
        Task<Dictionary<string, float>> Search(string[] tokens, bool exactMatch);
    }
}
