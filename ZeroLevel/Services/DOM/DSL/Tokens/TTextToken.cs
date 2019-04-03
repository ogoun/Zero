namespace DOM.DSL.Tokens
{
    /// <summary>
    /// Text token
    /// </summary>
    public class TTextToken :
        TToken
    {
        /// <summary>
        /// Constant text
        /// </summary>
        public string Text;

        public override TToken Clone()
        {
            return new TTextToken { Text = this.Text };
        }

        public override TToken CloneLocal()
        {
            return Clone();
        }
    }
}