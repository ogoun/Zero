namespace ZeroLevel.Patterns.Queries
{
    public interface IQuery
    {
        IQuery And(IQuery query);

        IQuery Or(IQuery query);

        IQuery Not();
    }
}