using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ZeroLevel.Sleopok.Engine.Services.Indexes
{
    public interface IIndexReader<T>
    {
        Task<IOrderedEnumerable<KeyValuePair<string, float>>> Search(string[] tokens, bool exactMatch);
        IAsyncEnumerable<FieldRecords> GetAll();
    }
}
