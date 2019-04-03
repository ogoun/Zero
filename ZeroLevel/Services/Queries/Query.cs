namespace ZeroLevel.Patterns.Queries
{
    public static class Query
    {
        public static IQuery EQ(string fieldName, object val)
        {
            return new QueryOp(fieldName, val, QueryOperation.EQ);
        }

        public static IQuery NEQ(string fieldName, object val)
        {
            return new QueryOp(fieldName, val, QueryOperation.NEQ);
        }

        public static IQuery GT(string fieldName, object val)
        {
            return new QueryOp(fieldName, val, QueryOperation.GT);
        }

        public static IQuery LT(string fieldName, object val)
        {
            return new QueryOp(fieldName, val, QueryOperation.LT);
        }

        public static IQuery IN(string fieldName, object val)
        {
            return new QueryOp(fieldName, val, QueryOperation.IN);
        }

        public static IQuery GTE(string fieldName, object val)
        {
            return new QueryOp(fieldName, val, QueryOperation.GTE);
        }

        public static IQuery LTE(string fieldName, object val)
        {
            return new QueryOp(fieldName, val, QueryOperation.LTE);
        }

        public static IQuery ALL()
        {
            return new QueryOp(string.Empty, null, QueryOperation.ALL);
        }
    }
}