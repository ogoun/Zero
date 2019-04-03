namespace ZeroLevel.Patterns.Queries
{
    public sealed class OrQuery : BaseQuery
    {
        public IQuery Left;
        public IQuery Right;

        public OrQuery(IQuery left, IQuery right)
        {
            this.Left = left;
            this.Right = right;
        }
    }
}