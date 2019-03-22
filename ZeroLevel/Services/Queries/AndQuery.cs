namespace ZeroLevel.Patterns.Queries
{
    public sealed class AndQuery : BaseQuery
    {
        public readonly IQuery Left;
        public readonly IQuery Right;

        public AndQuery(IQuery left, IQuery right)
        {
            this.Left = left;
            this.Right = right;
        }
    }
}
