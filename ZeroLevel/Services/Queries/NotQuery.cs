namespace ZeroLevel.Patterns.Queries
{
    public sealed class NotQuery : BaseQuery
    {
        public IQuery Query;

        public NotQuery(IQuery query)
        {
            this.Query = query;
        }
    }
}
