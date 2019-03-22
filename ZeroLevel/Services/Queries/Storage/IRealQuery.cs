namespace ZeroLevel.Patterns.Queries
{
    public interface IRealQuery<T, Q>
    {
        Q Query { get; }
    }
}
