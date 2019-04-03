namespace ZeroLevel.Patterns.Queries
{
    public sealed class QueryOp :
        BaseQuery
    {
        public readonly string FieldName;
        public readonly object Value;
        public readonly QueryOperation Operation;

        public QueryOp(string fieldName, object value, QueryOperation operation)
        {
            this.FieldName = fieldName;
            this.Value = value;
            this.Operation = operation;
        }
    }
}