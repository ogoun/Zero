namespace ZeroLevel.Patterns.Queries
{
    public enum QueryOperation:
        int
    {
        EQ = 0,
        NEQ = 1,
        GT = 2,
        LT = 3,
        GTE = 4,
        LTE = 5,
        IN = 6,
        ALL = 7
    }
}
