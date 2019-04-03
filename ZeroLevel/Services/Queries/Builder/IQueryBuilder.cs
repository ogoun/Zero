namespace ZeroLevel.Patterns.Queries
{
    public interface IQueryBuilder<T, Q>
    {
        /// <summary>
        /// Turning an abstract query into a real one, for a specific repository
        /// </summary>
        /// <param name="query">Abstract query</param>
        /// <returns>Request to store a specific type</returns>
        IRealQuery<T, Q> Build(IQuery query);
    }
}
