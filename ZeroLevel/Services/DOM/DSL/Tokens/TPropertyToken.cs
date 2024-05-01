namespace DOM.DSL.Tokens
{
    public class TPropertyToken :
        TToken
    {
        public string PropertyName;
        public TToken PropertyIndex;
        public TToken NextToken;

        public override TToken Clone()
        {
            return new TPropertyToken
            {
                PropertyIndex = this.PropertyIndex,
                PropertyName = this.PropertyName,
                NextToken = this.NextToken?.Clone()!
            };
        }

        public override TToken CloneLocal()
        {
            return new TPropertyToken
            {
                PropertyIndex = this.PropertyIndex,
                PropertyName = this.PropertyName,
                NextToken = null!
            };
        }
    }
}