namespace DOM.DSL.Tokens
{
    /// <summary>
    /// Токен ссылающийся на элемент документа
    /// </summary>
    public class TElementToken :
        TToken
    {
        /// <summary>
        /// Имя элемента
        /// </summary>
        public string ElementName;
        /// <summary>
        /// Опционально, при наличии свойств и/или функций для текущего элемента
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
