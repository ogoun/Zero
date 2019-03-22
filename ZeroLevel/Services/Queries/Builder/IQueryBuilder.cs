namespace ZeroLevel.Patterns.Queries
{
    public interface IQueryBuilder<T, Q>
    {
        /// <summary>
        /// Превращение абстрактного запроса в реальный, под конкретное хранилище
        /// </summary>
        /// <param name="query">Абстрактный запрос</param>
        /// <returns>Запрос к хранилищу конкретного типа</returns>
        IRealQuery<T, Q> Build(IQuery query);
    }
}
