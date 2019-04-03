namespace ZeroLevel.Patterns.Queries
{
    public class QueryResult
    {
        public bool Success { get; set; }
        public string Reason { get; set; }
        public long Count { get; set; }

        public static QueryResult Result(long count = 0)
        {
            return new QueryResult
            {
                Count = count,
                Success = true
            };
        }

        public static QueryResult Fault(string reason)
        {
            return new QueryResult
            {
                Reason = reason,
                Success = false
            };
        }
    }
}