namespace DOM.DSL.Tokens
{
    /// <summary>
    /// Token referring to the document element
    /// </summary>
    public class TElementToken :
        TToken
    {
        /// <summary>
        /// Имя элемента
        /// </summary>
        public string ElementName;

        /// <summary>
        /// Optionally, next token
        /// </summary>
        public TToken NextToken;

        public override TToken Clone()
        {
            return new TElementToken
            {
                ElementName = this.ElementName,
                NextToken = this.NextToken?.Clone()
            };
        }

        public override TToken CloneLocal()
        {
            return new TElementToken
            {
                ElementName = this.ElementName,
                NextToken = null
            };
        }
    }
}