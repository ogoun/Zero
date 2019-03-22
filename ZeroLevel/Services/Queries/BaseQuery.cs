namespace ZeroLevel.Patterns.Queries
{
    public abstract class BaseQuery :
        IQuery
    {
        public IQuery And(IQuery query)
        {
            return new AndQuery(this, query);
        }

        public IQuery Or(IQuery query)
        {
            return new OrQuery(this, query);
        }

        public IQuery Not()
        {
            return new NotQuery(this);
        }
    }
}
