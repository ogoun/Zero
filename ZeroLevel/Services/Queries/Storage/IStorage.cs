using System.Collections.Generic;

namespace ZeroLevel.Patterns.Queries
{
    public interface IStorage<T>
    {
        IEnumerable<T> Get();
        IEnumerable<T> Get(IQuery query);
        QueryResult Count();
        QueryResult Count(IQuery query);
        QueryResult Post(T obj);
        QueryResult Remove(T obj);
        QueryResult Remove(IQuery query);

        void Drop();
    }
}
