namespace DOM.DSL.Tokens
{
    /// <summary>
    /// Текстовый токен
    /// </summary>
    public class TTextToken :
        TToken
    {
        /// <summary>
        /// Текст в шаблоне, переносимый в результат без обработки
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
